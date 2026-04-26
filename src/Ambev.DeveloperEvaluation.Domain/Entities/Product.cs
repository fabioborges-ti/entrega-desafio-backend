namespace Ambev.DeveloperEvaluation.Domain.Entities;



/// <summary>

/// Produto do catálogo conforme <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/products-api.md">products-api</see>.

/// </summary>

public class Product

{

    public int Id { get; set; }



    public string Title { get; set; } = string.Empty;



    public decimal Price { get; set; }



    public string Description { get; set; } = string.Empty;



    public int CategoryId { get; set; }



    public Category? Category { get; set; }



    public string Image { get; set; } = string.Empty;

    public ICollection<ProductUserRating> UserRatings { get; set; } = new List<ProductUserRating>();

    public ICollection<CartLineItem> CartLineItems { get; set; } = new List<CartLineItem>();



    public Inventory? Inventory { get; set; }

}



