namespace senior_project_web.Models
{
    //購物車Model，不會於資料庫中建檔，僅用於顯示於網頁
    public class CartModel : Order_MealModel
    {
        public MealModel Meal { get; set; }
    }
}
