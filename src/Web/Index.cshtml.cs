using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web;

public class IndexModel : PageModel
{
    static JsonSerializerOptions jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GuidResponse Guid => new(System.Guid.NewGuid());

    public IActionResult OnGet()
    {
        var headers = HttpContext.Request.GetTypedHeaders();
        var mediaTypes = headers.Accept.ToList();
        if (headers.ContentType is not null)
            mediaTypes.Add(headers.ContentType);

        // try each Accept preference in order
        foreach (var accept in mediaTypes)
        {
            if (accept.MediaType == "application/json")
                return new JsonResult(Guid, jsonOptions);

            if (accept.MediaType == "text/plain")
                return new ContentResult { Content = Guid.D, ContentType = "text/plain" };

            if (accept.MediaType == "text/html")
                return Page();
        }

        // default to text/plain
        return new ContentResult { Content = Guid.D, ContentType = "text/plain" };
    }

    public class GuidResponse(Guid guid)
    {
        [JsonIgnore]
        public Guid Guid => guid;
        public string N => guid.ToString("N");
        public string D => guid.ToString("D");
        public string B => guid.ToString("B");
        public string P => guid.ToString("P");
        public string Motd => "Congratulations!";
    };
}