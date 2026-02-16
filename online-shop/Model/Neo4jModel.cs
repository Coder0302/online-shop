using System.Text.Json.Serialization;
using Npgsql.Replication;
using ZstdSharp.Unsafe;

namespace project.Models.Neo4jModels
{
    public enum TypeEdge
    {
        SHOWN,
        VIEWED = 1,
        LIKED = 2,
        PURCHASED = 3,
        BOUGHT_TOGETHER = 4,
        VISITED = 5,
        QUANTITY = 6
    }
    public enum TypeNode
    {
        USER = 1,
        PRODUCT = 2,
        STORE = 3
    }

    public abstract class Neo4jVoidEdge
    {   
        [JsonPropertyName("name")]
        public string Name { get; set; } 

        [JsonPropertyName("type")]
        public TypeEdge Type { get; set; }

        public TypeNode TypeNodeSrc { get; set; }
        public TypeNode TypeNodeDst { get; set; }
        public abstract Dictionary<string, object> ToProperties();
    }

    public abstract class Neo4jEdge : Neo4jVoidEdge
    {   
        [JsonPropertyName("Date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class ShownEdge : Neo4jEdge
    {
        public ShownEdge()
        {
            Name = "SHOWN";
            TypeNodeSrc = TypeNode.PRODUCT;
            TypeNodeDst = TypeNode.USER;
            Type = TypeEdge.SHOWN;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["date"] = Date
            };
        }
    }
    public class ViewedEdge : Neo4jEdge
    {
        public ViewedEdge()
        {
            Name = "VIEWED";
            TypeNodeSrc = TypeNode.USER;
            TypeNodeDst = TypeNode.PRODUCT;
            Type = TypeEdge.VIEWED;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["date"] = Date
            };
        }
    }

    public class LikedEdge : Neo4jEdge
    {
        public LikedEdge()
        {
            Name = "LIKED";
            TypeNodeSrc = TypeNode.USER;
            TypeNodeDst = TypeNode.PRODUCT;
            Type = TypeEdge.LIKED;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["Date"] = Date
            };
        }
    }

    public class PurchasedEdge : Neo4jEdge
    {
        public PurchasedEdge()
        {
            Name = "PURCHASED";
            TypeNodeSrc = TypeNode.USER;
            TypeNodeDst = TypeNode.PRODUCT;
            Type = TypeEdge.PURCHASED;
        }
        
        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            var properties = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["date"] = Date
            };
            
            if (Rating.HasValue)
            {
                properties["rating"] = Rating.Value;
            }
            
            return properties;
        }
    }
    public class BoughtTogetherEdge : Neo4jEdge
    {
        public BoughtTogetherEdge()
        {
            TypeNodeSrc = TypeNode.PRODUCT;
            TypeNodeDst = TypeNode.PRODUCT;
            Name = "BOUGHT_TOGETHER";
            Type = TypeEdge.BOUGHT_TOGETHER;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type
            };
        }
    }

    public class VisitedEdge : Neo4jEdge
    {
        public VisitedEdge()
        {
            Name = "VISITED";
            TypeNodeSrc = TypeNode.USER;
            TypeNodeDst = TypeNode.STORE;
            Type = TypeEdge.VISITED;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["date"] = Date
            };
        }
    }

    public class QuantityEdge : Neo4jEdge
    {
        public QuantityEdge()
        {
            Name = "QUANTITY";
            TypeNodeSrc = TypeNode.STORE;
            TypeNodeDst = TypeNode.PRODUCT;
            Type = TypeEdge.QUANTITY;
        }
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["type"] = (int)Type,
                ["quantity"] = Quantity,
                ["date"] = Date
            };
        }
    }

    public abstract class Neo4jNode
    {
        [JsonPropertyName("ext_id")]
        required public string Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("type")]
        public TypeNode Type { get; set; }
        
        public abstract Dictionary<string, object> ToProperties();
    }

    public static class Neo4jExtensions
    {
        public static string GetStringType(this Neo4jNode node)
        {
            return node.Type switch
            {
                TypeNode.USER => "User",
                TypeNode.PRODUCT => "Product",
                TypeNode.STORE => "Store",
                _ => "Node"
            };
        }
        public static int GetIntType(object typename)
        {
            return typename switch
            {
                "User" => (int)TypeNode.USER,
                "Product" => (int)TypeNode.PRODUCT,
                "Store" => (int)TypeNode.STORE,
                _ => 0
            };
        }
        
        public static string GetStringType<T>() where T : Neo4jNode
        {
            var tempNode = Activator.CreateInstance<T>();
            return tempNode.GetStringType();
        }
    }

    public class UserNode : Neo4jNode
    {
        public UserNode()
        {
            Type = TypeNode.USER;
        }

        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name ?? "TEST",
                ["type"] = (int)Type
            };
        }
    }

    public class VoidNode : UserNode;

    public class ProductNode : Neo4jNode
    {
        public ProductNode()
        {
            Type = TypeNode.PRODUCT;
        }
        
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public override Dictionary<string, object> ToProperties()
        {
            return new Dictionary<string, object>
            {
                ["type"] = (int)Type,
                ["name"] = Name ?? Id,
                ["tags"] = Tags,
                ["createdAt"] = CreatedAt
            };
        }
    }

    public class StoreNode : Neo4jNode
    {
        public StoreNode()
        {
            Type = TypeNode.STORE;
        }
        
        [JsonPropertyName("address")]
        required public string Address { get; set; }
        
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        public override Dictionary<string, object> ToProperties()
        {
            var props = new Dictionary<string, object>
            {
                ["type"] = (int)Type,
                ["name"] = Name ?? Id,
                ["address"] = Address,
                ["capacity"] = Capacity
            };
            
            return props;
        }
    }
}