﻿# Rest.Fody
A [Fody](https://github.com/Fody/Fody) addin, heavily inspired by [Refit](https://github.com/paulcbetts/refit) and [RestEase](https://github.com/canton7/RestEase).  
Thankfully, the source code for [ReactiveUI.Fody](https://github.com/kswoll/ReactiveUI.Fody) was easy to understand, and greatly helped me.

## Disclaimer
Right now, it **doesn't** work. It should be working (but *not* production-ready) within a few days.

## Basic syntax
````csharp
[ServiceFor("http://example.com/api/v2")]
public class API
{
    [Get("/user")]
    public extern async Task<MyUser> GetUser();
}
````

## Source structure

### Attributes.cs
#### Namespace: `Rest`
Attributes usable in the user's code.

### Constants.cs
#### Namespace: `Rest.Fody`
Constants, such as type & property names.

### ModuleWeavers.cs
#### Namespace: `Rest.Fody`
The entry point of the weaver. Defines basic methods.

### Weaving/Middleware.cs
#### Namespace: `Rest.Fody`

## API

### Basic anatomy of a class
```
[ServiceFor("http://example.com/api/v1")]
public class API
{
    public API()
    {
    }
    
    [Get("/user")]
    public extern Task<User> GetUser();
}
```
A class will be recognized and modified by Rest.Fody if either:
- It has the attribute `[ServiceFor(URL)]` ;
- It has the attribute `[Service]`, and contains a non-virtual HttpClient marked with the attribute `[RestClient]`.

In the first case, a private `HttpClient` field will be created, and be used internally.  
With the `[ServiceFor(URL)]` attribute, it is also possible to specify custom headers, via the
`[Header(name, value)]` attribute.

### Making requests
```
[Get("/")]
public extern Task CheckInternetConnection();
```
A request must be marked `extern`, and return either a `Task`, a `Task<T>`, or any object whose base class is `Task`.  
On failure, a request will throw a `RestException`, which contains the `StatusCode` and the `ReasonPhrase` of the failure.

### Deserialization / Serialization
To free itself of any dependency besides Fody and Mono.Cecil, Rest.Fody will not be able to deserialize or serialize types besides all numeric types, `Stream`, `string` and `byte[]`. When deserializing, `HttpResponseMessage` is also accepted.

To add your own (de)serialization, declare one of those methods in a static class marked with the `[]` attribute, or in the class itself:
- `string Serialize(object obj)` *or* `byte[] Serialize(object obj)`
- `T Deserialize<T>(string src)` *or* `T Deserialize<T>(byte[] bytes)`

### Query, dynamic url
```
[Get("/todo")]
public extern Task<List<Todo>> GetTodos([Query] int offset, [Query] int count);

[Post("/todo/{todoId}")]
public extern Task<Todo> SaveTodo(string todoId);

[Delete("/todo/{todoId}")]
public extern Task<Todo> DeleteTodo([Alias("todoId")] string id, [Query] string @if);
```
Four ways to specify query parameters:
- `[Query] T obj` or `[Query(name)] T obj`
- `[Query] IDictionary<string, T> query` or `[Query(name)] IDictionary<string, T> query`

If the name of the query starts with a '@', it will be removed.

Two ways to change a dynamic url:
- `string id`
- `[Alias(name)] string id`

### Body
```
[Put("/todo/{todoId}")]
public extern Task<Todo> UpdateTodo(string todoId, [Body] Todo todo);
```
The body of the request must be **unique**, and marked with the `[Body]` attribute.

### Headers
```
[ServiceFor("http://example.com/api/v1")]
[Header("Authorization", "Bearer xxx")]
public class API
{
    [Header("Authorization")]
    public extern Task DoSomethingWithoutAuthenticating([Header("X-Client")] string client);
}
```
Headers can be specified on both classes, methods and parameters:
- On classes, `[Header(name)]` will throw, and `[Header(name, value)]` will add a default header.
- On methods, `[Header(name)]` will remove a default header, and `[Header(name, value)]` will override or add a new header.
- On parameters, `[Header(name)]` will override or add a new header, and `[Header(name, value)]` will throw.

**Note**: Default headers specified on a class will be ignored if the class provides its own `HttpClient`.  
**Note**: A `[Headers]` attribute is valid on parameters that implements `IDictionary<string, string>`.

### Misc
- Yes, you can do `[Get("http://full.url/user")]`.
- You can add your own attributes if they inherit the `HttpMethodAttribute` and override its static property `Method` with the `new` keyword.
- Most checks are done on **build**, but that does not mean that it is perfectly safe.
