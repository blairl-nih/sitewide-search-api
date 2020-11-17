# sitewide-search-svc

unimportant change

## Configuration

The Sitewide Search Service uses a hierarchy of JSON files and environment variables to determine what configuration is
in effect. Settings in each layer replace any matching settings in the previous layer.  The layers are, in order,

1. appsettings.json
1. appsettings.${Environment}.json
1. Environment variables

For the **appsettings.${Environment}.json** file, value of the ASPNETCORE_ENVIRONMENT (e.g. Development, Staging, Production) is subsituted
for ${Environment}. These files are explicitly blocked in .gitignore in order to allow developers to have local json files containing passwords
and server names without worrying about accidentally pushing them to GitHub.

When using an environment variable to override a nested setting (e.g. The Servers property under elasticsearch), use __ (double underscore) to
mark the successive layers of nesting. (Technically, colon is also allowed, however colon is not an allowed character in environment variable
names under the bash shell.)

### Environment-specific Configuration Properties

**Elasticsearch**
```javascript
{
  "Elasticsearch" : {
    "Servers" : "http://localhost", // Comma-separated list of elasticsearch servers
    "Userid" : "no-userid",         // Userid for authenticating 
    "Password" : "no-password"      // Password for authenticating      
  }
}
```
As environment variables:
```bash
export Elasticsearch__Servers=http://localhost
export Elasticsearch__Userid=no-userid
export Elasticsearch__Password=no-password
```
(Use either export or set, depending on the operating system.)

The Elasticsearch:Servers property is required to contain URIs for one or more Elasticsearch servers.
Each URI must include a protocol (http or https), a server name, and optionally, a port number.
Multiple URIs are separated by a comma.  (e.g. "https://fred:9200, https://george:9201, https://ginny:9202")



Reference: [Microsoft Configuration introduction](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

## Build
```
cd sitewide-search-api

# install NuGet packages
dotnet restore
# builds all projects (test are dependent on src)
dotnet build
# runs unit tests (only in tests folder)
dotnet test test/**
# Run code coverage 
# (../../lcov is because this will run from the test project...)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=../../lcov test/**
# Publish to publish directory
dotnet publish -c Release -o ../../publish src/NCI.OCPL.Api.SiteWideSearch
```


## API URL
Once site wide search has been deployed, the API may be accessed via a URL of the form

```
http://<<server_name>[:<<port_number>>/search/<<collection>>/<<language>>/<<search_string>>
```

Where:
* **<< server_name >>** is the host server name.
* **<< port_number >>** is the (optional) port number.
* **<< collection >>** is either "cgov" to get search results for the CancerGov site or "doc" to get results for a Division, Office, or Center site.
* **<< language >>** is either "en" or "es"  (and "es" is only allowed when the collection is "cgov")
* **<< search_string >>** is the URL-encoded string to search for.

So, for example:
```http://webapis.cancer.gov/search/cgov/en/tumor```
