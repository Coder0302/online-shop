using project.Models.Neo4jModels;
using Neo4j.Driver;
using project.Models.Neo4jModels.Responses;

namespace project.Services
{
    public interface INeo4jService
    {
        Task<T> CreateNodeAsync<T>(T node) where T : Neo4jNode;
        Task<T> GetNodeAsync<T>(string nodeId) where T : Neo4jNode;
        Task<bool> UpdateNodeAsync<T>(string nodeId, T node) where T : Neo4jNode;
        Task<bool> DeleteNodeAsync(string nodeId);
        
        Task<TRel> CreateEdgeAsync<TRel>(string fromNodeId, string toNodeId, TRel Edge) 
            where TRel : Neo4jEdge;
        Task<List<TRel>> GetEdgesAsync<TRel>(string nodeId) where TRel : Neo4jEdge;
        Task<bool> DeleteEdgeAsync(string EdgeId);
        
        Task<List<T>> GetNodesByTypeAsync<T>() where T : Neo4jNode;
        Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null);
        
        Task SeedTestDataAsync(int users = 30, int products = 50, int stores = 10);
        Task<List<ProductNode>> GetViewedProductsByUserAsync(string userId);
        Task<List<UserNode>> GetUsersWhoLikedProductAsync(string productId);
        Task<List<ProductNode>> GetRecommendedProductsAsync(string userId);
        Task<List<ProductNode>> GetBoughtTogetherProductsAsync(string productId);
        Task<Dictionary<string, int>> GetProductAvailabilityInStoresAsync(string productId);
    }
    public class Neo4jService : INeo4jService
    {
        private IDriver _context;

        public Neo4jService(IDriver context)
        {
            _context = context;
        }

        public async Task<bool> test()
        {
            Console.WriteLine("TEST1728");

            return true;
        }

        public async Task<T> CreateNodeAsync<T>(T node) where T : Neo4jNode
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<T> GetNodeAsync<T>(string nodeId) where T : Neo4jNode
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<bool> UpdateNodeAsync<T>(string nodeId, T node) where T : Neo4jNode
        {
            Console.WriteLine("TEST1728");
            return true;
        }
        public async Task<bool> DeleteNodeAsync(string nodeId)
        {
            Console.WriteLine("TEST1728");
            return true;
        }
        
        public async Task<TRel> CreateEdgeAsync<TRel>(string fromNodeId, string toNodeId, TRel Edge) 
            where TRel : Neo4jEdge
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<TRel>> GetEdgesAsync<TRel>(string nodeId) where TRel : Neo4jEdge
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<bool> DeleteEdgeAsync(string EdgeId)
        {
            Console.WriteLine("TEST1728");
            return true;
        }
        
        public async Task<List<T>> GetNodesByTypeAsync<T>() where T : Neo4jNode
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        
        public async Task SeedTestDataAsync(int users = 30, int products = 50, int stores = 10)
        {
            Console.WriteLine("TEST1728");
        }
        public async Task<List<ProductNode>> GetViewedProductsByUserAsync(string userId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<UserNode>> GetUsersWhoLikedProductAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<ProductNode>> GetRecommendedProductsAsync(string userId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<ProductNode>> GetBoughtTogetherProductsAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<Dictionary<string, int>> GetProductAvailabilityInStoresAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
    }
}