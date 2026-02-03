using System.Text.Json.Serialization;

namespace project.Models.Neo4jModels
{
    public abstract class Neo4jEdge
    {   
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("Date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
        
        public abstract Dictionary<string, object> ToProperties();
    }

    public abstract class Neo4jVoidEdge
    {   
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        public abstract Dictionary<string, object> ToProperties();
    }

    public class ViewedEdge : Neo4jEdge
    {
        public ViewedEdge()
        {
            Name = "VIEWED";
            Type = "VIEWED";
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type,
                ["date"] = Date
            };
        }
    }

    public class LikedEdge : Neo4jEdge
    {
        public LikedEdge()
        {
            Name = "LIKED";
            Type = "LIKED";
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type,
                ["Date"] = Date
            };
        }
    }

    public class PurchasedEdge : Neo4jEdge
    {
        public PurchasedEdge()
        {
            Name = "PURCHASED";
            Type = "PURCHASED";
        }
        
        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            var properties = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type,
                ["date"] = Date
            };
            
            if (Rating.HasValue)
            {
                properties["rating"] = Rating.Value;
            }
            
            return properties;
        }
    }
    public class BoughtTogetherEdge : Neo4jVoidEdge
    {
        public BoughtTogetherEdge()
        {
            Name = "BOUGHT_TOGETHER";
            Type = "BOUGHT_TOGETHER";
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type
            };
        }
    }

    public class VisitedEdge : Neo4jEdge
    {
        public VisitedEdge()
        {
            Name = "VISITED";
            Type = "VISITED";
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type,
                ["date"] = Date
            };
        }
    }

    public class QuantityEdge : Neo4jEdge
    {
        public QuantityEdge()
        {
            Name = "QUANTITY";
            Type = "QUANTITY";
        }
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = Type,
                ["quantity"] = Quantity,
                ["date"] = Date
            };
        }
    }

    public abstract class Neo4jNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        public abstract Dictionary<string, object> ToProperties();
    }

    public class UserNode : Neo4jNode
    {
        public UserNode()
        {
            Type = "User";
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type,
                ["name"] = Name
            };
        }
    }

    public class ProductNode : Neo4jNode
    {
        public ProductNode()
        {
            Type = "Product";
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type,
                ["name"] = Name,
                ["tags"] = Tags,
                ["createdAt"] = CreatedAt
            };
        }
    }

    public class StoreNode : Neo4jNode
    {
        public StoreNode()
        {
            Type = "Store";
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("address")]
        public string Address { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            var props = new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type,
                ["name"] = Name,
                ["address"] = Address,
                ["capacity"] = Capacity
            };
            
            return props;
        }
    }
}