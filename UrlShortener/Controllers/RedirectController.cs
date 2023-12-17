using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.Controllers;

[ApiController]
[Route("", Name = "Redirect")]
public class RedirectController : ControllerBase
{

    public RedirectController ()
    {
    }

    [HttpGet("{alias}", Name = "Redirect")]
    public IActionResult Get(string alias)
    {
        return Redirect("https://www.google.com" + alias);
    }

}
