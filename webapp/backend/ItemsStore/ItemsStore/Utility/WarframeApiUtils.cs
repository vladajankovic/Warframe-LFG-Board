using ItemsStore.Models;
using System.Text.RegularExpressions;

namespace ItemsStore.Utility
{
    public class WarframeApiUtils
    {
        private readonly HttpClient _httpClient;
        private readonly string baseUrl = "https://api.warframestat.us/items";
        private const string ResourcePagePath = "../resource_page.html";

        public WarframeApiUtils(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ItemDTO>> GetAllItems()
        {
            var allItems = await _httpClient.GetFromJsonAsync<List<ItemDTO>>(baseUrl);
            return allItems ?? [];
        }

        public async Task<List<RelicDTO>> GetAllRelics()
        {
            var allItems = await _httpClient.GetFromJsonAsync<List<RelicDTO>>(baseUrl);
            if (allItems == null) return [];
            var relics = allItems.Where(item => item.Type == "Relic" && item.Name.EndsWith("Intact")).ToList();
            return relics;
        }

        public Task<List<WikiResourceItemDTO>> GetAllResourcesFromWiki()
        {
            var htmlPath = Path.GetFullPath(ResourcePagePath);
            var html = File.ReadAllText(htmlPath);
            var resources = ParseWikiResources(html);
            return Task.FromResult(resources);
        }

        private static List<WikiResourceItemDTO> ParseWikiResources(string html)
        {
            var results = new List<WikiResourceItemDTO>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var tabPattern = new Regex(
                @"class=""tabbertab"" data-title=""([^""]+)""(.*?)(?=class=""tabbertab"" data-title=|$)",
                RegexOptions.Singleline);

            var navboxPattern = new Regex(
                @"<table class=""navbox"">(.*?)</table>",
                RegexOptions.Singleline);

            var titlePattern = new Regex(
                @"class=""navbox-title"".*?<th[^>]*>(.*?)</th>",
                RegexOptions.Singleline);

            var groupPattern = new Regex(
                @"class=""navbox-group""[^>]*>(.*?)</th>\s*<td[^>]*>(.*?)</td>",
                RegexOptions.Singleline);

            var resourcePattern = new Regex(@"data-param-name=""([^""]+)""");

            foreach (Match tabMatch in tabPattern.Matches(html))
            {
                var region = tabMatch.Groups[1].Value.Trim();
                var tabBody  = tabMatch.Groups[2].Value;

                foreach (Match navboxMatch in navboxPattern.Matches(tabBody))
                {
                    var navboxBody = navboxMatch.Groups[1].Value;

                    string? category = null;
                    var titleMatch = titlePattern.Match(navboxBody);
                    if (titleMatch.Success)
                        category = StripHtml(titleMatch.Groups[1].Value);

                    foreach (Match groupMatch in groupPattern.Matches(navboxBody))
                    {
                        var group    = StripHtml(groupMatch.Groups[1].Value);
                        var listBody = groupMatch.Groups[2].Value;

                        foreach (Match resMatch in resourcePattern.Matches(listBody))
                        {
                            var name = resMatch.Groups[1].Value.Trim();
                            if (!seen.Add(name)) continue;

                            results.Add(new WikiResourceItemDTO
                            {
                                Name = name,
                                Region = region,
                                Category = category,
                                Group = group
                            });
                        }
                    }
                }
            }

            return results;
        }

        private static string StripHtml(string html)
        {
            var text = Regex.Replace(html, @"<[^>]+>", " ");
            text = Regex.Replace(text, @"&(?:nbsp|#160);", " ");
            text = Regex.Replace(text, @"&amp;",  "&");
            text = Regex.Replace(text, @"&lt;",   "<");
            text = Regex.Replace(text, @"&gt;",   ">");
            text = Regex.Replace(text, @"&#39;|&apos;", "'");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }
}
