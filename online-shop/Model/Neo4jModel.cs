using System.Text.Json.Serialization;

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
        required public string Name { get; set; }

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
                ["type"] = Type,
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
            TypeNodeSrc = TypeNode.USER;
            TypeNodeDst = TypeNode.PRODUCT;
            Type = TypeEdge.LIKED;
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
                ["type"] = Type
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
                ["type"] = Type,
                ["quantity"] = Quantity,
                ["date"] = Date
            };
        }
    }

    public abstract class Neo4jNode
    {
        [JsonPropertyName("id")]
        required public string Id { get; set; }
        [JsonPropertyName("name")]
        required public string Name { get; set; }
        
        [JsonPropertyName("type")]
        public TypeNode Type { get; set; }
        
        public abstract Dictionary<string, object> ToProperties();
    }

    public class UserNode : Neo4jNode
    {
        public UserNode(string _name, string _id)
        {
            Id = _id;
            Name = _name;
            Type = TypeNode.USER;
        }

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
        public ProductNode(string _name, string _id)
        {
            Id = _id;
            Name = _name;
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
        public StoreNode(string _name, string _address)
        {
            Name = _name;
            Address = _address;
            Type = TypeNode.STORE;
        }
        
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