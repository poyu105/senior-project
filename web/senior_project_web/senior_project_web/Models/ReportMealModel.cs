﻿namespace senior_project_web.Models
{
    //報表與餐點多對多中間表
    public class ReportMealModel
    {
        public Guid meal_id {  get; set; }
        public MealModel Meal { get; set; }

        public int report_id { get; set; }
        public Daily_Sales_ReportModel Report { get; set; }
    }
}