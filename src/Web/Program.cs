using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

static Task Result(HttpContext context, string result, string contentType)
{
    context.Response.ContentType = contentType;
    return context.Response.WriteAsync(result);
}

app.MapGet("/", context =>
{
    GuidResponse res = new(Guid.NewGuid());

    var headers = context.Request.GetTypedHeaders();
    var mediaTypes = headers.Accept.ToList();
    if (headers.ContentType is not null)
        mediaTypes.Add(headers.ContentType);

    // try each Accept preference in order
    foreach (var accept in mediaTypes)
    {
        if (accept.MediaType == "application/json")
            return Result(context, JsonSerializer.Serialize(res, jsonOptions), "application/json");

        if (accept.MediaType == "text/plain")
            return Result(context, res.Guid.ToString("D"), "text/plain");

        if (accept.MediaType == "text/html")
            return Result(context,
                //lang=html
                $$$"""
                <html>
                    <head>
                        <title>New GUID!</title>
                        <style>
                            body { font-family: system-ui, "Segoe UI", "Ubuntu", "Roboto", "Noto Sans", "Droid Sans", sans-serif; }
                            body, footer { margin: 0.5rem;}
                            footer { position: fixed; bottom: 0; left: 0; }

                            h1 { margin-bottom: 1rem; }
                            dl { font-family: ui-monospace, "Segoe UI Mono", "Liberation Mono", Menlo, Monaco, Consolas, monospace; }
                            dl { display: grid; grid-template-columns: 1rem auto; gap: 1rem 0.5rem; }
                            dt { font-weight: bold; }
                            dd { margin: 0; }
                            dd.in { margin-left: 1ch; }

                            p { 
                              font-style: italic;
                              animation: ease 1s forwards congrats;
                              opacity: 0;
                              animation-delay: 1s;
                            }
                            @keyframes congrats { from { opacity: 0; } to { opacity: 1; } }

                            // thank you EM: https://meyerweb.com/eric/thoughts/2023/01/16/minimal-dark-mode-styling/
                            @media (prefers-color-scheme: dark) { html { filter: invert(1); } }
                        </style>
                    </head>
                    <body>
                        <h1>New Guid</h1>
                        <dl>
                            <dt>D</dt><dd class="in">{{{res.D}}}</dd>
                            <dt>N</dt><dd class="in">{{{res.N}}}</dd>
                            <dt>B</dt><dd>{{{res.B}}}</dd>
                            <dt>P</dt><dd>{{{res.P}}}</dd>
                        </dl>
                        <p>Congratulations!</p>
                        <footer><a href="https://www.wassupy.com">Wassupy</a></footer>
                    </body>
                </html>
                """, "text/html");
    }

    // default to text/plain if no Accept or Content-Type header matches
    return Result(context, res.Guid.ToString("D"), "text/plain");
});

app.Run();



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