# URL Shortening Service

A URL Shortening service like TinyURL with .Net.

This project mainly focus on the application service itself rather than the whole high availablity archtecture.

## Requirements Analysis

### Functional Requirements

- Given a long url, generate a shorter and unique alias of it.
- When user access a short link, redirect them to the original link.
- User should be able to pickup a custom short link for their url.
- Links will be expire after a standard default timespan and users are able to define the expiration time.

## API Design

### Create a short link

**POST** `/app/api/createUrl`

`Content-Type: application/json`

Example payload:

```json
{
    // required
    "originalUrl": "https://github.com/LeuisKen/url-shortening-service",
    // optional
    "customAlias": "mysvc",
    // optional, timestamp
    "expireDate": 1702828649311
}
```

Response:

`200 OK`: The link created successfully.

```json
{
    "status": 0,
    "msg": "",
    "data": {
        "url": "http://<host>:<port>/mysvc"
    }
}
```

`400 Bad Request`: The custom alias conflict with existing short links.

```json
{
    "status": 400,
    "msg": "The custom alias conflict with existing short links.",
}
```

`500 Internal Server Error`: The server encountered an unexpected condition which prevented it from fulfilling the request.

```json
{
    "status": 500,
    "msg": "Please contact the administrator for help.",
}
```

### Use a short link

**GET** `/{alias}`

Response:

`302 Found`: Redirect to the original url.

`404 Not Found`: The short link does not exist.

`400 Bad Request`: The short link has expired.

## Database Design

Use dynamodb as the database since there is actually no relationship between data entities.

### Table: Url

| Field | Type | Description |
| --- | --- | --- |
| alias | string | The alias of the url. |
| originalUrl | string | The original url. |
| createTime | number | The create time of the url. |
| expireDate | number | The expire date of the url. |

## Run the service

Check the `demo.ipynb` for more details.

You can configure the service by modifying the `appsettings.json` file or setting the environment variables. Check the `appsettings.json` and `docker-compose.yml` for more details.

## Known Limitations

### Alias Generation Ahead of Time

It is possible to make the alias generation offline. For example, we can create a separate web service which only focus on alias generation, and the main service will call the alias generation service to generate the alias. By having the new web service, we can generate the alias ahead of time and store them in the database. When the main service receives a request, it can just query the database to get the alias. This can reduce the latency of the main service.

However, this will introduce a new problem with concurrency. Since the alias generation service is offline, it is possible that multiple requests will generate the same alias. We can solve this problem by using a lock mechanism.

And this will introduce another problem: the alias generation service will become a single point of failure. If the alias generation service is down, the main service will not be able to generate any new alias. And we need to create more replicas of the alias generation service to increase the availability.

To manage multiple replicas of the alias generation service, we will introduce cordination complexity and also health check complexity. And we will also need to introduce a load balancer to distribute the requests to different replicas. To keep the simplicity of the service, I will not implement this feature.
