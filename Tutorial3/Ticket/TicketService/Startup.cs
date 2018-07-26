using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace TicketService
{
    public class Startup
	{
		// This method gets called by the runtime. Use it to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Add TicketContext to the IoC container.
			string connection = @"Server=sql.data;Database=TicketDB;User=sa;Password=<!Passw0rd>;";
			services.AddDbContext<TicketContext>(options => options.UseSqlServer(connection));
		}

		// This method gets called by the runtime. Use it to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.Run(async (context) =>
			{
				// Extract id from request
				string idString = context.Request.Query["id"].FirstOrDefault();
				int id = int.TryParse(idString, out int result) ? result : -1;
				// Build the response
				string barcode = app.ApplicationServices.GetService<TicketContext>()
									.Tickets.Find(id).BarCode;
				string hostname = System.Net.Dns.GetHostName();
				string html = "<h3>Hi Visitor!</h3>" +
				$"Here is your ticket: <b>{barcode}</b><br/>" +
				$"Brought to you by: <b>{hostname}</b><br/>";
				await context.Response.WriteAsync(html);
			});
		}
	}
}
