using GymApp.Modules.Nutrition.Domain.Entities;

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Seed;

public static class EgyptianFoodSeedData
{
    public static IReadOnlyList<FoodDatabase> GetAll()
    {
        return new[]
        {
            Create("Ful Medames", "فول مدمس", 200, "g", 187, 13, 30, 1, "Legumes"),
            Create("Koshari", "كشري", 300, "g", 350, 12, 65, 5, "Main Dishes"),
            Create("Ta'ameya (Falafel)", "طعمية (فلافل)", 100, "g", 333, 13, 31, 18, "Main Dishes"),
            Create("Grilled Chicken", "دجاج مشوي", 150, "g", 231, 35, 0, 10, "Protein"),
            Create("Grilled Kofta", "كفتة مشوية", 150, "g", 290, 25, 3, 20, "Protein"),
            Create("Shawarma", "شاورما", 200, "g", 420, 28, 30, 22, "Main Dishes"),
            Create("Molokhia", "ملوخية", 250, "g", 85, 5, 10, 3, "Main Dishes"),
            Create("Mahshi", "محشي", 200, "g", 280, 8, 35, 12, "Main Dishes"),
            Create("Fattah", "فتة", 300, "g", 450, 20, 45, 20, "Main Dishes"),
            Create("Rice", "أرز", 150, "g", 195, 4, 43, 0.5m, "Grains"),
            Create("Bread (Aish Baladi)", "عيش بلدي", 80, "g", 210, 7, 42, 1, "Grains"),
            Create("Lentil Soup", "شوربة عدس", 300, "ml", 180, 12, 30, 1, "Soups"),
            Create("Scrambled Eggs", "بيض مقلي", 100, "g", 212, 14, 2, 16, "Protein"),
            Create("Boiled Eggs", "بيض مسلوق", 50, "g", 78, 6, 0.5m, 5, "Protein"),
            Create("Greek Yogurt", "زبادي يوناني", 150, "g", 146, 20, 6, 4, "Dairy"),
            Create("Feta Cheese", "جبنة فيتا", 30, "g", 75, 4, 1, 6, "Dairy"),
            Create("Dates", "تمر", 40, "g", 111, 1, 30, 0.1m, "Fruits"),
            Create("Banana", "موز", 120, "g", 107, 1, 27, 0.4m, "Fruits"),
            Create("Orange", "برتقال", 150, "g", 70, 1, 18, 0.2m, "Fruits"),
            Create("Mango", "مانجو", 165, "g", 99, 1, 25, 0.6m, "Fruits"),
            Create("Watermelon", "بطيخ", 280, "g", 84, 2, 21, 0.4m, "Fruits"),
            Create("Cucumber", "خيار", 100, "g", 15, 1, 3, 0.1m, "Vegetables"),
            Create("Tomato", "طماطم", 100, "g", 18, 1, 4, 0.2m, "Vegetables"),
            Create("Mixed Salad", "سلطة خضراء", 150, "g", 30, 2, 5, 0.3m, "Vegetables"),
            Create("Hummus", "حمص", 100, "g", 166, 8, 14, 10, "Dips"),
            Create("Baba Ghanoush", "بابا غنوج", 100, "g", 72, 2, 5, 5, "Dips"),
            Create("Grilled Fish (Tilapia)", "سمك مشوي (بلطي)", 150, "g", 162, 34, 0, 3, "Protein"),
            Create("Grilled Shrimp", "جمبري مشوي", 100, "g", 99, 21, 0, 1, "Protein"),
            Create("Liver (Kebda)", "كبدة", 150, "g", 224, 30, 5, 9, "Protein"),
            Create("Sausage (Sogo')", "سجق", 100, "g", 301, 14, 2, 26, "Protein"),
            Create("Sweet Potato", "بطاطا حلوة", 150, "g", 135, 2, 31, 0.1m, "Vegetables"),
            Create("Potato", "بطاطس", 150, "g", 116, 3, 27, 0.1m, "Vegetables"),
            Create("Pasta", "مكرونة", 200, "g", 262, 9, 50, 2, "Grains"),
            Create("Oatmeal", "شوفان", 40, "g", 152, 5, 27, 3, "Grains"),
            Create("Corn Flakes", "كورن فليكس", 30, "g", 112, 2, 25, 0.3m, "Grains"),
            Create("Peanut Butter", "زبدة فول سوداني", 32, "g", 190, 7, 7, 16, "Nuts"),
            Create("Almonds", "لوز", 30, "g", 173, 6, 6, 15, "Nuts"),
            Create("Walnuts", "عين جمل", 30, "g", 196, 5, 4, 20, "Nuts"),
            Create("Honey", "عسل", 21, "g", 64, 0.1m, 17, 0, "Sweeteners"),
            Create("Molasses (Asal Eswed)", "عسل أسود", 20, "g", 58, 0.1m, 15, 0, "Sweeteners"),
            Create("Halawa", "حلاوة طحينية", 30, "g", 157, 4, 16, 9, "Desserts"),
            Create("Konafa", "كنافة", 100, "g", 390, 6, 45, 20, "Desserts"),
            Create("Basbousa", "بسبوسة", 80, "g", 320, 4, 40, 16, "Desserts"),
            Create("Qatayef", "قطايف", 100, "g", 350, 7, 45, 15, "Desserts"),
            Create("Milk", "لبن", 250, "ml", 149, 8, 12, 8, "Dairy"),
            Create("Tea", "شاي", 250, "ml", 2, 0, 0.5m, 0, "Beverages"),
            Create("Coffee", "قهوة", 250, "ml", 5, 0.3m, 0, 0, "Beverages"),
            Create("Fresh Juice", "عصير فريش", 250, "ml", 112, 1, 26, 0.3m, "Beverages"),
            Create("Sugarcane Juice", "عصير قصب", 250, "ml", 183, 0, 46, 0, "Beverages"),
            Create("Hibiscus (Karkade)", "كركديه", 250, "ml", 37, 0, 9, 0, "Beverages"),
        };
    }

    private static FoodDatabase Create(
        string name,
        string nameArabic,
        decimal servingSize,
        string servingUnit,
        decimal calories,
        decimal protein,
        decimal carbs,
        decimal fat,
        string category)
    {
        return FoodDatabase.Create(
            name,
            nameArabic,
            servingSize,
            servingUnit,
            calories,
            protein,
            carbs,
            fat,
            category);
    }
}
