using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Areas.Admin.Models;
using System.Globalization;

namespace PhamTaManhLan_8888.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly ApplicationDbContext _dbContext;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
            _apiUrl = configuration["Gemini:ApiUrl"];
            _dbContext = dbContext;

            if (string.IsNullOrEmpty(_apiUrl) || !Uri.IsWellFormedUriString(_apiUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException("API URL không hợp lệ. Kiểm tra GeminiAI: ApiUrl trong appsettings.json.");
            }
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API Key không hợp lệ. Kiểm tra GeminiAI: ApiKey trong appsettings.json.");
            }
        }

        public async Task<string> GetAIResponse(string input)
        {
            try
            {
                // Lấy dữ liệu từ database hoặc trả về mặc định
                string context = await GetDatabaseContext(input);
                // Gửi yêu cầu tới Gemini API với dữ liệu từ database
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"Dựa trên dữ liệu sau: {context}\nCâu hỏi:{input}" }
                            }
                        }
                    }
                };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var requestUrl = $"{_apiUrl}?key={_apiKey}";
                Console.WriteLine($"Request URL: {requestUrl}");
                var response = await _httpClient.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API call failed:{response.StatusCode} - {errorContent}");
                }
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseString);
                return result.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        // Hàm nhỏ: Lấy dữ liệu từ database dựa trên input
        private async Task<string> GetDatabaseContext(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "Không có câu hỏi để phân tích.";
            }
            input = input.ToLower();
            if (_dbContext == null || _dbContext.Products == null)
            {
                return "Không thể truy cập cơ sở dữ liệu.";
            }

            // Câu hỏi về sản phẩm rẻ nhất
            if (input.Contains("rẻ nhất"))
            {
                return await GetCheapestProduct();
            }
            // Câu hỏi về sản phẩm đắt nhất
            else if (input.Contains("đắt nhất"))
            {
                return await GetMostExpensiveProduct();
            }
            // Câu hỏi về danh sách sản phẩm
            else if (input.Contains("sản phẩm"))
            {
                return await GetProductList();
            }
            // Câu hỏi về bánh sinh nhật
            else if (input.Contains("bánh sinh nhật"))
            {
                return await GetBirthdayCake();
            }
            // Câu hỏi về bánh kem
            else if (input.Contains("bánh kem"))
            {
                return await GetCake();
            }
            // Câu hỏi về bánh quy
            else if (input.Contains("bánh quy"))
            {
                return await GetCookie();
            }
            // Câu hỏi về bánh mì
            else if (input.Contains("bánh mì"))
            {
                return await GetBread();
            }
            return "Không có dữ liệu liên quan trong database.";
        }

        // Hàm nhỏ: Lấy sản phẩm rẻ nhất
        private async Task<string> GetCheapestProduct()
        {
            var cheapestProduct = await _dbContext.Products
                .OrderBy(p => p.Price)
                .FirstOrDefaultAsync();
            if (cheapestProduct != null)
            {
                return $"Sản phẩm rẻ nhất là {cheapestProduct.Name} với giá {cheapestProduct.Price:N0} VND.";
            }
            return "Không tìm thấy sản phẩm nào.";
        }

        // Hàm nhỏ: Lấy sản phẩm đắt nhất
        private async Task<string> GetMostExpensiveProduct()
        {
            var mostExpensiveProduct = await _dbContext.Products
                .OrderByDescending(p => p.Price)
                .FirstOrDefaultAsync();
            if (mostExpensiveProduct != null)
            {
                return $"Sản phẩm đắt nhất là {mostExpensiveProduct.Name} với giá {mostExpensiveProduct.Price:N0} VND.";
            }
            return "Không tìm thấy sản phẩm nào.";
        }

        // Lấy danh sách sản phẩm
        private async Task<string> GetProductList()
        {
            var products = await _dbContext.Products.ToListAsync();
            if (products != null && products.Any())
            {
                return $"Danh sách sản phẩm: {string.Join(", ", products.Select(p => $"{p.Name} - {p.Price:N0} VND"))}";
            }
            return "Không có sản phẩm nào trong cơ sở dữ liệu.";
        }
        // Lấy bánh sinh nhật:
        private async Task<string> GetBirthdayCake()
        {
            var birthdayCake = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Name.Contains("sinh nhật"));
            if (birthdayCake != null)
            {
                return $"Bánh sinh nhật của tiệm là {birthdayCake.Name} với giá {birthdayCake.Price:N0} VND.";
            }
            return "Không có bánh sinh nhật nào.";
        }
        //Lấy bánh kem:
        private async Task<string> GetCake()
        {
            var cake = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Name.Contains("kem"));
            if (cake != null)
            {
                return $"Bánh kem của tiệm là {cake.Name} với giá {cake.Price:N0} VND.";
            }
            return "Không có bánh kem nào.";
        }
        //Lấy bánh quy:
        private async Task<string> GetCookie()
        {
            var cookie = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Name.Contains("quy"));
            if (cookie != null)
            {
                return $"Bánh quy của tiệm là {cookie.Name} với giá {cookie.Price:N0} VND.";
            }
            return "Không có bánh quy nào.";
        }

        //Lấy bánh quy:
        private async Task<string> GetBread()
        {
            var bread = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Name.Contains("mì"));
            if (bread != null)
            {
                return $"Bánh mì của tiệm là {bread.Name} với giá {bread.Price:N0} VND.";
            }
            return "Không có bánh mì nào.";
        }

    }

}
