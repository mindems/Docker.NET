#!/bin/bash

set -e

if [ "$1" = '/opt/mssql/bin/sqlservr' ]; then
  # Initialize the database on first run
  if [ ! -f /tmp/app-initialized ]; then
    function initialize_app_database() {
    # Wait for SQL Server to start
	printf "Connecting to SQL Server"
	until $(nc -z localhost 1433); do
	  printf '.'
	  sleep 2
	done      
	  # Create and seed the database
      /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "<!Passw0rd>" \
	    -i /usr/src/app/ticketDb.sql
      # Note the container has been initialized
      touch /tmp/app-initialized
    }
    # Call the function asynchronously
    initialize_app_database &
  fi
fi

exec "$@"