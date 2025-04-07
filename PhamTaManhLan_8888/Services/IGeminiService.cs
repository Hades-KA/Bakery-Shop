using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PhamTaManhLan_8888.Models;

namespace PhamTaManhLan_8888.Services
{
    public interface IGeminiService
    {
        Task<string> GetAIResponse(string input);
    }


}
