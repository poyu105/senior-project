namespace orderSys_bk.Model.Dto
{
    public class UpdateMealDto
    {
        public Guid? id { get; set; }
        public string? name { get; set; }
        public string? type { get; set; }
        public string? description { get; set; }
        public int cost { get; set; }
        public int price { get; set; }
        public string? img_path { get; set; }
        public int quantity { get; set; }
    }
}
