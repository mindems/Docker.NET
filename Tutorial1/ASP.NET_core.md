# Tutorial 1 - ASP.NET core



#### Introduction

In the first part of this tutorial trilogy our goal is to create a simple *TicketService* using Docker containers.



#### Prerequisites

This tutorial assumes you have Windows 10 Pro and [.NET core](https://www.microsoft.com/net/learn/get-started/windows) installed. The professional edition is mandatory as we'll be using [Docker](https://www.docker.com/community-edition) for container deployment. Given the .NET core framework evolves quickly, it's worth noting these tutorials are written and tested against .NET core 2.1 (your results may vary).



#### The Application

Create a new ASP.NET Core application in a directory called *TicketService* by executing the following command in PowerShell *(tip: launch PowerShell with WIN+X, A)*.

```
dotnet new web -o TicketService
```

Let's go over the distinct parts that make up the command:

- The `dotnet` command runs the tools necessary for .NET development. Each verb executes a different command. 
- The `dotnet new` command is used to create .NET Core projects. 
- The `-o TicketService` option after the `dotnet new` command is used to give the location to create the ASP.NET Core application. 
- For this microservice, we want the simplest, most lightweight web application possible, so we used the "ASP.NET Core Empty" template, by specifying its short name, `web`.

The template creates three files for you:

- A *Startup.cs* file. This contains the basis of the application.
- A *Program.cs* file. This contains the entry point of the application.
- A *TicketService.csproj* file. This is the build file for the application.

 You can now run the template generated application:

```
cd TicketService
```

```
dotnet run
```

The last command will first restore dependencies required to build the application and then it will build the application.

The default configuration listens to `http://localhost:5000`. You can open a browser and navigate to that page to see a "Hello World!" message.

When you're done, you can shut down the application by pressing *Ctrl+C*.



#### The Microservice 

The service we're going to build delivers *Tickets*. The barcodes are implemented as [Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid) strings and for our current sample, we'll generate these at random.

In the spirit of *less is more* the `Ticket` class is kept rather simple. Add *Ticket.cs* to the project folder. 

```
namespace TicketService
{
    public class Ticket
    {
        public int Id { get; private set; }
        public string BarCode { get; private set; }

        static public Ticket FindBy(int id)
        {
            return new Ticket() { Id = id, BarCode = System.Guid.NewGuid().ToString() };
        }
    }
}
```

The `Id` property increments with each sold `Ticket` and holds the current count. As mentioned, our implementation of `BarCode` uses a string representation of a `Guid`. We let a static method handle the random assignment. In the final part of this series, we will swap the random generator with a data store.

In *Startup.cs* you can now swap the HelloWorld implementation with the code below.

```
app.Run(async (context) =>
{
    // Extract id from request
    string idString = context.Request.Query["id"].FirstOrDefault();
    int id = int.TryParse(idString, out int result) ? result : -1;
    // Build the response
    string barcode = Ticket.FindBy(id).BarCode;
    string hostname = System.Net.Dns.GetHostName();
    string html = "<h3>Hi Visitor!</h3>" +
    $"Here is your ticket: <b>{barcode}</b><br/>" +
    $"Brought to you by: <b>{hostname}</b><br/>";
    await context.Response.WriteAsync(html);
});
```

To start the application:

```
dotnet run
```

Depending on the IDE you're using, the final output in *PowerShell* will look similar to:

```
Now listening on: http://localhost:50091
Application started. Press Ctrl+C to shut down.
```

Note opening the project in Visual Studio will add *Properties\launchSettings.json*, which can remap the ports  (*applicationUrl*).

You can test your service by opening a browser and navigating to the address in *your output*, and specifying an `id`. For *my output*, that would be `http://localhost:50091/?id=1`.



#### The Docker image

Our final task is to run the application in Docker. We'll create a Docker container that runs a Docker image.

A **Docker Image** is an immutable file that defines the environment for running the application.

A **Docker Container** represents a running instance of a Docker Image.

By analogy, you can think of the *Docker Image* as a *class*, and the *Docker Container* as an object, or an instance of that class.  

Create a file without a file extension, name it *Dockerfile*, and place it in the project folder *TicketService*.

```
FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TicketService.dll"]
```

Let's go over its contents.

The first line specifies the source image used for building the application:		

```
FROM microsoft/dotnet:2.1-sdk AS build
```

Docker allows you to configure a machine image based on a source template. That means you don't have to supply all the machine parameters when you start, you only need to supply any changes to the template. The changes here will be to include our application.

In this sample, we'll use the `2.1-sdk` version of the `dotnet` image. This is the easiest way to create a working Docker environment. This image includes the .NET Core runtime, and the .NET Core SDK. That makes it easier to get started and build, but does create a larger image. We will be using this image for building the application and a different image to run it.

The next lines setup and build your application:		

```
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out
```

This will copy the project file from the  current directory to the Docker VM, and restore all the packages. Using the dotnet CLI means that the Docker image must include the .NET Core SDK. After that, the rest of your application gets copied, and the `dotnet publish` command builds and packages your application.

Finally, we create a second Docker image that runs the application:		

```
# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TicketService.dll"]
```

This image uses the `2.1-aspnetcore-runtime` version of the `dotnet` image, which contains everything necessary to run ASP.NET Core applications, but does not include the .NET Core SDK. This means this image can't be used to build .NET Core applications, but it also makes the final image smaller. Runtime images can also contain pre-jitted packages, improving startup times.

To make this work, we copy the built application from the first image to the second one.

The `ENTRYPOINT` tells Docker how to start the service.



#### Building and running the image in a container

Let's build an image and run the service inside a Docker container. You don't want all the files from your local directory copied into the image. Instead, you'll want to build the application in the container. Start by creating a **.dockerignore** file *(tip: when using a text editor, save the file as `.dockerignore.`--note the final dot)* to specify the directories that are not copied into the image. You don't want any of the build assets copied. Specify the build and publish directories in the .dockerignore file.		

```
bin/*
obj/*
out/*
```

You build the image using the `docker build` command. Run the following command from the project folder.			

```
docker build -t ticketservice-image .
```

This command builds the container image based on all the information in your Dockerfile. The `-t` argument provides a tag, or name, for this container image. In the command line above, the tag used for the Docker container is `ticketservice-image`. When this command completes, you'll be ready to run your container.  

Run the following command to start the container and launch your service:

```
docker run -p 80:80 --name ticketservice ticketservice-image
```

The `-p` option indicates the port mapping between the service and the host. Here it says that any incoming request on port 80 should be forwarded to port 80 on the container. Using 80 matches the port the service is listening on, which is the default port for production applications. The `--name` argument names your running container. It's a convenient name you can use to reference the container. 

At the time of writing the output to the command is similar to:

```
PS> docker run -p 80:80 --name ticketservice ticketservice-image
Error:
  An assembly specified in the application dependencies manifest (TicketService.deps.json) was not found:
    package: 'Microsoft.ApplicationInsights.AspNetCore', version: '2.1.1'
    path: 'lib/netstandard1.6/Microsoft.ApplicationInsights.AspNetCore.dll'
  This assembly was expected to be in the local runtime store as the application was published using the following target manifest files:
    aspnetcore-store-2.0.0-linux-x64.xml;aspnetcore-store-2.0.0-osx-x64.xml;aspnetcore-store-2.0.0-win7-x64.xml;aspnetcore-store-2.0.0-win7-x86.xml
```

We can resolve the error by editing *TicketService.csproj* and adding the following `PropertyGroup`:

```
<PropertyGroup>
    <PublishWithAspNetCoreTargetManifest>false</PublishWithAspNetCoreTargetManifest>
</PropertyGroup>
```

You'll need to remove the container and start over with a clean slate. 

```
docker rm ticketservice
```

Now, rebuild the image and launch the container.

```
docker build -t ticketservice-image .
```

```
docker run -p 80:80 --name ticketservice ticketservice-image
```

 This time the output ends with:

```
Now listening on: http://[::]:80
Application started. Press Ctrl+C to shut down.
```

Note the port in the output (when you launch a container) is always the port the container is listening on, but not necessarily the one your host uses. To recall the mapping you used when launching the container, you can use the command:

```
docker port ticketservice
```

In this case we're using port 80 on both ends, meaning you can test your service by opening a browser and navigating to localhost, remember to specify an id:

`http://localhost/?id=1`

*Notice anything different about the response message?*



#### Running a Container in Detached Mode

We want to be able to run containers in the background, so they don't terminate with a PowerShell session. This is where the detached mode comes into play. First, we need to stop and remove the container attached to PowerShell.

```
docker stop ticketservice
```

```
docker rm ticketservice
```

Launch a new container, this time adding the `-d` option.

```
docker run -d -p 80:80 --name ticketservice ticketservice-image
```

The `-d` option is used to run the container detached from the current terminal. That means you won't see the command output in PowerShell. Instead, a long string is returned when control is given back to the terminal. This alphanumeric string is the container id.

 You can see if the image is running by checking the command:		

```
docker ps
```

If your container is running, you'll see a line that lists it in the running processes. (It may be the only one.) The container id (in its abbreviated form) is listed first. It is the same id as in our response message, the `hostname` in our ticketing service.

When you ran your service attached to PowerShell, you could see diagnostic information printed for each request. You don't see that information when your container is running in detached mode. The Docker attach command enables you to attach to a running container so that you can see the log information.  

Run this command from a *new* PowerShell window:	

```
docker attach --sig-proxy=false ticketservice
```

The `--sig-proxy=false` argument means that *Ctrl+C* commands do not get sent to the container process, but rather stop the `docker attach` command. The final argument is the name given to the container in the `docker run` command. 

You can also use any (first part of a) Docker assigned container id to refer to its container. You'll have to do so if you didn't specify a name for your container in `docker run`.

Open a browser and navigate to your service (use the same address as before). You'll see the diagnostic messages in the new PowerShell from the attached running container.

Press *Ctrl+C* to stop the attach process.

When you are done working with your container, you can stop it:

```
docker stop ticketservice
```

The container is still available for you to restart, until you remove it from your machine:

```
docker rm ticketservice
```

If you want you can also remove the image from your machine:

```
docker rmi ticketservice-image
```



#### Conclusion

In this tutorial, you built an ASP.NET Core microservice.

You built a Docker container image for that service, and ran that container on your machine.



#### Resources

- https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/microservices



#### Next

[Tutorial 2 - docker-compose](../Tutorial2/docker-compose.md)