### About
This API is intended to document the available service endpoints and provide limited example usage to assist developers in building outside programs that leverage the same data sources in an effort to minimize duplicative efforts across the USGS and other agencies.

### Status
Something about the status of project
### Getting Started
The Wavelab services perform multiple procedures which include:  1.  Providing a json time series to assess the best start and end dates for sea pressure or barometric pressure data.  2.  Conversion of barometric pressure csv file to a netCDF file. 3.  Conversion of sea pressure csv file to a netCDF file and creation of water level and wave statistics netCDF files and visualizations. The responses are intended to be directly consumed by custom client applications and used to more fully decouple the client-service relationship by providing directional hypermedia links within the resource response objects.

The data is extracted from both a barometric pressure and sea pressure csv file with their respective timeseries data.  Their respective metadata is compiled from user input so the netCDF file contains all the metadata necessary to be CF compliant.  This will allow the netCDF file to be used in many applications including a THREDDS server.  Afterwards, water level is computed using the hydrostatic assumption.  Then using a Butterworth filter the wave signal is filtered out to get a Storm Tide signal.  Finally, wave statistics will be computed for data that is sampled at 4hz or more.  This is done using linear wave theory, spectral analysis, and engineering statistics.  The data is then output in netCDF files and hydrographs.

The Wavelab Services API performs procedures in order to output netCDF files and jpeg visualizations. As documented by this page, which can also serve as a simple URL builder, the Wavelab Services API is built following [RESTful](http://en.wikipedia.org/wiki/Representational_state_transfer) principles to ensure scalability and predictable URLs. JSON is returned by all API responses, including errors and example results and summaries for each resource. This API is intended to provide some guidance in working with the services, however some methods may only provide visual examples due to authentication requirements or limitations of the API documentation application.

### Using the API
The URL and an example response can be obtained by accessing one of the following resources and uri endpoint located on the sidebar (or selecting the menu button located on the bottom of the screen, if viewing on a mobile device). 

<img alt="sidebarImage.jpg" 
src="" />

The sidebar displays the available service resources, accompanying HTTP method and URI endpoints. Selecting a URL endpoint will display additional resource documentation information related to the selected endpoint. Such as;

The description of the resource
The Service URL.
Any URL Parameters.
Parameter Name,
Value Type,
A description of what the parameter represents,
Whether the parameter is required or optional,
A REST Query URL test tool is also available that builds an example url, based on the user provided input parameter values and a simple query tool that provides an example of the requested response.

### Common Status Codes
Common HTTP status codes returned by the services

| Code &nbsp; &nbsp; &nbsp;   |When     
| ------- |---------
| `200`   | Status  `OK`.
| `400`   | Status `Bad Request`. When the request data cannot be read.
| `404`   | Status `Not Found`. When resource is not available.
| `500`   | Status `Internal Server Error`. Please contact the administrator. 

### Example
Web service request can be performed using most HTTP client libraries. The following illustrated a typical http request/response performed by a client application.

```
GET /WaveLabservices HTTP/1.1
Host: streamstats.usgs.gov
Accept: application/json
```
```
HTTP/1.1 200 OK

  
```
