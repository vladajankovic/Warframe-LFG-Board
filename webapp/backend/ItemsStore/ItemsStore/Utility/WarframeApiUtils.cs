using ItemsStore.Models;

namespace ItemsStore.Utility
{
    public class WarframeApiUtils
    {
        private readonly HttpClient _httpClient;
        private readonly string baseUrl = "https://api.warframestat.us/items";

        public WarframeApiUtils(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ItemDTO>> GetAllItems()
        {
            var allItems = await _httpClient.GetFromJsonAsync<List<ItemDTO>>(baseUrl);

            return allItems ?? [];
        }

        public async Task<List<ItemDTO>> GetAllRelics()
        {
            var allItems = await _httpClient.GetFromJsonAsync<List<ItemDTO>>(baseUrl);

            if (allItems == null) return [];

            var relics = allItems.Where(item => item.Type == "Relic" && item.Name.EndsWith("Intact")).ToList();

            return relics;
        }

        public async Task<List<ItemDTO>> GetAllResources()
        {
            var allItems = await _httpClient.GetFromJsonAsync<List<ItemDTO>>(baseUrl);

            if (allItems == null) return [];

            var resources = allItems.Where(item => 
                item.Type == "Resource" && 
                item.UniqueName.StartsWith("/Lotus/Types/") && 
                !item.ExcludeFromCodex &&
                !string.IsNullOrEmpty(item.Description)
            ).ToList();

            return resources;
        }
    }
}
