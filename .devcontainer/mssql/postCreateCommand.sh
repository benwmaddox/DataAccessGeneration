#!/bin/bash
dacpac="false"
sqlfiles="false"
SApassword=$1
dacpath=$2
sqlpath=$3

echo "SELECT * FROM SYS.DATABASES" | dd of=testsqlconnection.sql
for i in {1..60};
do
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SApassword -d master -i testsqlconnection.sql > /dev/null
    if [ $? -eq 0 ]
    then
        echo "SQL server ready"
        break
    else
        echo "Not ready yet..."
        sleep 1
    fi
done
rm testsqlconnection.sql

#/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SApassword -d master -i ./.devcontainer/mssql/setup.sql 
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SApassword -d master -i ./DataAccessGeneration.IntegrationTest/Northwind/instnwnd.sql
