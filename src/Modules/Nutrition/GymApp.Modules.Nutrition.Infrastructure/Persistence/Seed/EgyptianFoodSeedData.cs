using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Seed;

public static class EgyptianFoodSeedData
{
    private static readonly (string Name, string NameArabic, decimal ServingSize, string ServingUnit,
        decimal Calories, decimal Protein, decimal Carbs, decimal Fat, string Category)[] Foods =
    {
        ("Ful Medames", "فول مدمس", 200, "g", 187, 13, 30, 1, "Legumes"),
        ("Koshari", "كشري", 300, "g", 350, 12, 65, 5, "Main Dishes"),
        ("Ta'ameya (Falafel)", "طعمية (فلافل)", 100, "g", 333, 13, 31, 18, "Main Dishes"),
        ("Grilled Chicken", "دجاج مشوي", 150, "g", 231, 35, 0, 10, "Protein"),
        ("Grilled Kofta", "كفتة مشوية", 150, "g", 290, 25, 3, 20, "Protein"),
        ("Shawarma", "شاورما", 200, "g", 420, 28, 30, 22, "Main Dishes"),
        ("Molokhia", "ملوخية", 250, "g", 85, 5, 10, 3, "Main Dishes"),
        ("Mahshi", "محشي", 200, "g", 280, 8, 35, 12, "Main Dishes"),
        ("Fattah", "فتة", 300, "g", 450, 20, 45, 20, "Main Dishes"),
        ("Rice", "أرز", 150, "g", 195, 4, 43, 0.5m, "Grains"),
        ("Bread (Aish Baladi)", "عيش بلدي", 80, "g", 210, 7, 42, 1, "Grains"),
        ("Lentil Soup", "شوربة عدس", 300, "ml", 180, 12, 30, 1, "Soups"),
        ("Scrambled Eggs", "بيض مقلي", 100, "g", 212, 14, 2, 16, "Protein"),
        ("Boiled Eggs", "بيض مسلوق", 50, "g", 78, 6, 0.5m, 5, "Protein"),
        ("Greek Yogurt", "زبادي يوناني", 150, "g", 146, 20, 6, 4, "Dairy"),
        ("Feta Cheese", "جبنة فيتا", 30, "g", 75, 4, 1, 6, "Dairy"),
        ("Dates", "تمر", 40, "g", 111, 1, 30, 0.1m, "Fruits"),
        ("Banana", "موز", 120, "g", 107, 1, 27, 0.4m, "Fruits"),
        ("Orange", "برتقال", 150, "g", 70, 1, 18, 0.2m, "Fruits"),
        ("Mango", "مانجو", 165, "g", 99, 1, 25, 0.6m, "Fruits"),
        ("Watermelon", "بطيخ", 280, "g", 84, 2, 21, 0.4m, "Fruits"),
        ("Cucumber", "خيار", 100, "g", 15, 1, 3, 0.1m, "Vegetables"),
        ("Tomato", "طماطم", 100, "g", 18, 1, 4, 0.2m, "Vegetables"),
        ("Mixed Salad", "سلطة خضراء", 150, "g", 30, 2, 5, 0.3m, "Vegetables"),
        ("Hummus", "حمص", 100, "g", 166, 8, 14, 10, "Dips"),
        ("Baba Ghanoush", "بابا غنوج", 100, "g", 72, 2, 5, 5, "Dips"),
        ("Grilled Fish (Tilapia)", "سمك مشوي (بلطي)", 150, "g", 162, 34, 0, 3, "Protein"),
        ("Grilled Shrimp", "جمبري مشوي", 100, "g", 99, 21, 0, 1, "Protein"),
        ("Liver (Kebda)", "كبدة", 150, "g", 224, 30, 5, 9, "Protein"),
        ("Sausage (Sogo')", "سجق", 100, "g", 301, 14, 2, 26, "Protein"),
        ("Sweet Potato", "بطاطا حلوة", 150, "g", 135, 2, 31, 0.1m, "Vegetables"),
        ("Potato", "بطاطس", 150, "g", 116, 3, 27, 0.1m, "Vegetables"),
        ("Pasta", "مكرونة", 200, "g", 262, 9, 50, 2, "Grains"),
        ("Oatmeal", "شوفان", 40, "g", 152, 5, 27, 3, "Grains"),
        ("Corn Flakes", "كورن فليكس", 30, "g", 112, 2, 25, 0.3m, "Grains"),
        ("Peanut Butter", "زبدة فول سوداني", 32, "g", 190, 7, 7, 16, "Nuts"),
        ("Almonds", "لوز", 30, "g", 173, 6, 6, 15, "Nuts"),
        ("Walnuts", "عين جمل", 30, "g", 196, 5, 4, 20, "Nuts"),
        ("Honey", "عسل", 21, "g", 64, 0.1m, 17, 0, "Sweeteners"),
        ("Molasses (Asal Eswed)", "عسل أسود", 20, "g", 58, 0.1m, 15, 0, "Sweeteners"),
        ("Halawa", "حلاوة طحينية", 30, "g", 157, 4, 16, 9, "Desserts"),
        ("Konafa", "كنافة", 100, "g", 390, 6, 45, 20, "Desserts"),
        ("Basbousa", "بسبوسة", 80, "g", 320, 4, 40, 16, "Desserts"),
        ("Qatayef", "قطايف", 100, "g", 350, 7, 45, 15, "Desserts"),
        ("Milk", "لبن", 250, "ml", 149, 8, 12, 8, "Dairy"),
        ("Tea", "شاي", 250, "ml", 2, 0, 0.5m, 0, "Beverages"),
        ("Coffee", "قهوة", 250, "ml", 5, 0.3m, 0, 0, "Beverages"),
        ("Fresh Juice", "عصير فريش", 250, "ml", 112, 1, 26, 0.3m, "Beverages"),
        ("Sugarcane Juice", "عصير قصب", 250, "ml", 183, 0, 46, 0, "Beverages"),
        ("Hibiscus (Karkade)", "كركديه", 250, "ml", 37, 0, 9, 0, "Beverages"),
    };

    public static IReadOnlyList<FoodDatabase> GetAll()
    {
        return Foods.Select(Create).ToArray();
    }

    private static FoodDatabase Create(
        (string Name, string NameArabic, decimal ServingSize, string ServingUnit,
            decimal Calories, decimal Protein, decimal Carbs, decimal Fat, string Category) food)
    {
        return FoodDatabase.Create(new FoodDatabaseDetails
        {
            Name = food.Name,
            NameArabic = food.NameArabic,
            ServingSize = food.ServingSize,
            ServingUnit = food.ServingUnit,
            CaloriesPerServing = food.Calories,
            ProteinPerServing = food.Protein,
            CarbsPerServing = food.Carbs,
            FatPerServing = food.Fat,
            Category = food.Category
        });
    }
}
