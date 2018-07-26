# Tutorial 2 - docker-compose



#### Introduction

In this second part of the series we compose and scale our app using a *docker-compose.yml* file.



#### Prerequisites

This tutorial assumes you have Windows 10 Pro and [.NET core](https://www.microsoft.com/net/learn/get-started/windows) installed. The professional edition is mandatory as we'll be using [Docker](https://www.docker.com/community-edition) for container deployment. Given the .NET core framework evolves quickly, it's worth noting these tutorials are written and tested against .NET core 2.1 (your results may vary).

In the previous tutorial we ran a ticketservice container, built from our custom Docker image. In this tutorial you can continue using your local image. If you want to tag along, without having to go through the first tutorial, you can pull the image from my public registry (as explained shortly). 



#### docker-compose

A `docker-compose.yml` file is a *YAML* file that defines how Docker containers should behave. 

```
version: "3"
services:
  ticket:
    image: ticketservice-image
    build: .
    ports:
      - "4000:80"
    depends_on:
      - sql.data
  sql.data:
    image: "microsoft/mssql-server-linux"
    environment:
      SA_PASSWORD: "<!Passw0rd>"
      ACCEPT_EULA: "Y"
```

In this file (similar to the one used in the next tutorial) we compose our `ticket` service with an *SQL server* service, each running in its own container. We tell Docker to use our local image `ticketservice-image` or, if the image isn't there, to build it from the *Dockerfile* in the *TicketService* project folder. 

Next, we forward any incoming request on port 4000 to port 80 on the container and tell Docker the `ticket` service depends on the `sql.data` service, indicating the order in which the services should be initialized.

We then use the `microsoft/mssql-server-linux` image to create the `sql.data` service. If there is no local image by that name, Docker will try to pull it from the public registry: 

```
sql.data:
  image: "microsoft/mssql-server-linux"
```

This format follows the convention:

```
myService:
  image: username/repo:tag
```

The last lines in the docker-compose set the necessary environment variables for the SQL container.

To launch the app, using PowerShell, you can navigate to the project directory and execute the command `docker-compose up`. Of course, we would need to modify our codebase to use SQL server first, which is what we'll do in the last tutorial.



#### Going through stages

To ease deployment the *YAML* is often divided over two files.

***docker-compose.yml*** files are used to hold static variables that are consistent across all builds.

***docker-compose.override.yml*** files are typically used for non-static variables, such as those that differ from the development to production stage. Have a look at a refactoring of the previous code sample.

```
# docker-compose.yml 
version: "3"
services:
  ticket:
    image: ticketservice-image
    depends_on:
      - sql.data
  sql.data:
    image: "microsoft/mssql-server-linux"
```

```
# docker-compose.override.yml 
version: "3"
services:
  ticket:
    build: .
    ports:
      - "4000:80"
    environment:
      DEBUG: 'true'
  sql.data:
    environment:
      SA_PASSWORD: "<!Passw0rd>"
      ACCEPT_EULA: "Y"
```

Going from development to production is as easy as swapping the *docker-compose.override.yml* file with the one below.

```
# docker-compose.production.yml 
version: "3"
services:
  ticket:
    ports:
      - 80:80
    environment:
      PRODUCTION: 'true'
```

This separation clearly has its perks, though for our simple demo I'll stick to using just *docker-compose.yml*.



#### Docker Namespaces

When you run a container, Docker creates a set of *namespaces* for that container. These namespaces provide layers of isolation. Each aspect of a container runs in a separate namespace and its access is limited to that namespace.

Furthermore, containers each have their own network namespace by default. Compose will place all containers on a shared network and set an alias in DNS for the service name. So to connect between containers, all you need to do is point to the service name.

Translated back to the example where we compose our ticketservice, with a SQL Server running in its own container. Given we named the server service `sql.data`, the connection string used in our ticketservice  would look like `"Server=sql.data;Database=master;User=sa;Password=<!Passw0rd>;"` during development.



#### Scaling the App

Time to get our hands dirty, create the following `docker-compose.yml` file anywhere you want.

```
# docker-compose.yml 
version: "3"
services:
  ticket:
    image: ticketservice-image
    deploy:
      replicas: 5
      resources:
        limits:
          cpus: "0.1"
          memory: 50M
      restart_policy:
        condition: on-failure
    ports:
      - "4000:80"
    networks:
      - webnet
networks:
  webnet:
```

*Note the `build: .` option doesn't play well with the `docker stack deploy` command we're about to use.*

This *docker-compose.yml* file views only one service, `ticket`, but let's us scale it the way we want. The first part of the file involves our local build. It tells Docker to use the local image (built in the previous tutorial).

```
ticket:
  image: ticketservice-image
```

If you choose to pull the image from the public registry, you can use the following line instead:

```
ticket:
  image: mindems/docker:tutorial1
```

The remainder of the `docker-compose.yml` file tells Docker to: 

- Run 5 instances of that image as a service called `ticket`, limiting each one to use, at most, 10% of the CPU (across all cores), and 50MB of RAM.
- Immediately restart containers if one fails.
- Map port 4000 on the host to `ticket`’s port 80.
- Instruct `ticket`’s containers to share port 80 via a load-balanced network called `webnet`. (Internally, the containers themselves publish to `ticket`’s port 80 at an ephemeral port.)
- Define the `webnet` network with the default settings (which is a load-balanced overlay network).

  

#### Run your new load-balanced app

Before we can use the `docker stack deploy` command we must first run:  

```
docker swarm init
```

Now let’s run it. You need to give your app a name. Here, it is set to `scaled`:

```
docker stack deploy -c docker-compose.yml scaled  
```

Our single service stack is running 5 container instances of our deployed image on one host. Let’s investigate. 

Get the service id for the one service in our application.  

```
docker service ls
```

Look for output for the `ticket` service, prepended with your app name. If you named it the same as shown in this example, the name is `scaled_ticket`. The service id is listed as well, along with the number of replicas, image name, and exposed ports.

A single container running in a service is called a *task*. Tasks are given unique ids that numerically increment, up to the number of `replicas` you defined in `docker-compose.yml`. List the tasks for your service:

```
docker service ps scaled_ticket
```

Tasks also show up if you just list all the containers on your system, though that is not filtered by service: 

```
docker container ls -q  
```

Open up your browser and visit `http://localhost:4000/?id=1` on multiple tabs. The container id will change, demonstrating the load-balancing; with each request, one of the 5 tasks is chosen, in a round-robin fashion, to respond. The container ids match your output from the previous command (`docker container ls -q`).

  

#### Reconfiguring the App

You can scale the app by changing the `replicas` value in `docker-compose.yml`, saving the change, and re-running the `docker stack deploy` command:

```
docker stack deploy -c docker-compose.yml scaled
```

Docker performs an in-place update, no need to tear the stack down first or kill any containers.

Now, re-run `docker container ls -q` to see the deployed instances reconfigured. If you scaled up the replicas, more tasks, and hence, more containers, are started.

  

#### Take down the App and the Swarm

- Take the app down with `docker stack rm`:

  ```
  docker stack rm scaled
  ```

- Take down the swarm.

  ```
  docker swarm leave --force
  ```



#### Conclusion

It’s as easy as that to compose and scale your app with Docker.

We've taken a short detour to prep ourselves for the next tutorial. This tutorial relied heavily on one provided by Docker, feel free to browse their [getting started guide](https://docs.docker.com/get-started/) to gain a deeper understanding.



#### Resources

- https://docs.docker.com/get-started/part3/
- https://docs.docker.com/compose/extends/
- https://docs.docker.com/engine/docker-overview/#the-underlying-technology



#### Next

[Tutorial 3 - SQL Server container](../Tutorial3/SQL_Server_container.md)