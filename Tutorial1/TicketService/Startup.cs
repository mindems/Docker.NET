using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TicketService
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
                string barcode = Ticket.FindBy(id).BarCode;
                string hostname = System.Net.Dns.GetHostName();
                string html = "<h3>Hi Visitor!</h3>" +
                $"Here is your ticket: <b>{barcode}</b><br/>" +
                $"Brought to you by: <b>{hostname}</b><br/>";
                await context.Response.WriteAsync(html);
            });
        }
    }
}
