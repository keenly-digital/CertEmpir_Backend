using Newtonsoft.Json.Linq;

public static class JsonPaginator
{
    public static JArray Paginate(string jsonString, int questionsPerPage = 10)
    {
        // 1) Parse root and then parse the ITEMS string into a real JArray
        var root = JObject.Parse(jsonString);
        var itemsRaw = (string)root["Data"]?["items"]
                          ?? throw new InvalidOperationException("Data.items not found or not a string");
        var items = JArray.Parse(itemsRaw);

        // 2) Normalize any newline characters in every string leaf
        void Normalize(JToken tok)
        {
            switch (tok.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)tok).Properties())
                        Normalize(prop.Value);
                    break;
                case JTokenType.Array:
                    foreach (var elt in (JArray)tok)
                        Normalize(elt);
                    break;
                case JTokenType.String:
                    var s = tok.Value<string>()!;
                    // replace both CR and LF with spaces
                    tok.Replace(new JValue(s.Replace("\r", " ").Replace("\n", " ")));
                    break;
                default:
                    break;
            }
        }
        Normalize(items);

        // 3) Flatten into a linear list of JObjects
        var flat = new List<JObject>();
        foreach (JObject it in items)
        {
            var type = (string)it["type"];
            switch (type)
            {
                case "topic":
                    var topic = (JObject)it["topic"];
                    flat.Add(new JObject(
                        new JProperty("type", "topic"),
                        new JProperty("topic", new JObject(
                            new JProperty("id", topic["id"]),
                            new JProperty("fileId", topic["fileId"]),
                            new JProperty("title", topic["title"])
                        ))
                    ));
                    foreach (JObject ti in (JArray)topic["topicItems"]!)
                        flat.Add((JObject)ti.DeepClone());
                    break;

                case "caseStudy":
                    var cs = (JObject)it["caseStudy"];
                    flat.Add(new JObject(
                        new JProperty("type", "caseStudy"),
                        new JProperty("caseStudy", new JObject(
                            new JProperty("id", cs["id"]),
                            new JProperty("fileId", cs["fileId"]),
                            new JProperty("title", cs["title"])
                        ))
                    ));
                    foreach (JObject csi in (JArray)cs["caseStudyItems"]!)
                        flat.Add((JObject)csi.DeepClone());
                    break;

                case "question":
                    flat.Add((JObject)it.DeepClone());
                    break;

                default:
                    // skip unknown types
                    break;
            }
        }

        // 4) Build your pages
        var pages = new JArray();
        var current = new JArray();
        int qCount = 0;

        foreach (var obj in flat)
        {
            bool isQ = ((string)obj["type"]) == "question";

            // if we've hit 10 questions, break to a new page
            if (isQ && qCount == questionsPerPage)
            {
                pages.Add(current);
                current = new JArray();
                qCount = 0;
            }
            // also break before a header if we just hit the limit
            else if (!isQ && qCount == questionsPerPage)
            {
                pages.Add(current);
                current = new JArray();
                qCount = 0;
            }

            current.Add(obj);
            if (isQ) qCount++;
        }

        if (current.Count > 0)
            pages.Add(current);

        return pages;
    }
}