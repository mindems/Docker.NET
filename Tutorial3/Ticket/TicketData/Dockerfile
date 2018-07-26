FROM microsoft/mssql-server-linux

# Install netcat and clean up APT when done
RUN apt-get update && apt-get install -y netcat \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /usr/src/app
COPY . ./

ENTRYPOINT [ "/bin/bash", "./entrypoint.sh" ]
CMD [ "/opt/mssql/bin/sqlservr" ]