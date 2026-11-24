using GymApp.Modules.Nutrition.Domain.Diet;
using GymApp.Modules.Nutrition.Domain.Enums;
using static GymApp.Modules.Nutrition.Application.Diets.DietFoods;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>
/// The ready Egyptian diets a coach gives a new member next to their training plan: everyday
/// home food (فول، بيض، عيش بلدي، فراخ، رز، ملوخية، عدس)، not imported "diet food".
/// </summary>
internal static class ReadyDietCatalog
{
    private static DietMeal Meal(MealType type, string ar, string en, params DietPortion[] portions) => new(type, ar, en, portions);

    private static DietMeal Breakfast(string ar, string en, params DietPortion[] p) => Meal(MealType.Breakfast, ar, en, p);
    private static DietMeal Snack(string ar, string en, params DietPortion[] p) => Meal(MealType.Snack, ar, en, p);
    private static DietMeal Lunch(string ar, string en, params DietPortion[] p) => Meal(MealType.Lunch, ar, en, p);
    private static DietMeal Dinner(string ar, string en, params DietPortion[] p) => Meal(MealType.Dinner, ar, en, p);

    private static readonly ReadyDiet FatLoss = new("fat_loss_high_protein",
        new DietText("تخسيس بأكل مصري – بروتين عالي", "Fat loss on Egyptian food – high protein",
            "٥ وجبات بيتي: بروتين عالي يشبّعك وكارب محسوب، من غير حرمان.",
            "Five home meals: filling high protein, measured carbs, no starving."),
        DietGoal.FatLoss,
        new[]
        {
            Breakfast("الفطار: فول وبيض", "Breakfast: ful & eggs", Ful.Of(150), Egg.Of(2), Bread.Of(0.5m), Cucumber.Of(100)),
            Snack("سناك: زبادي وبرتقال", "Snack: yogurt & orange", Yogurt.Of(170), Orange.Of(1)),
            Lunch("الغدا: فراخ ورز وسلطة", "Lunch: chicken, rice & salad", Chicken.Of(200), Rice.Of(150), Salad.Of(200)),
            Snack("سناك: لوز", "Snack: almonds", Almonds.Of(20)),
            Dinner("العشا: جبنة قريش وخيار", "Dinner: cottage cheese & cucumber", Cottage.Of(200), Bread.Of(0.5m), Cucumber.Of(150)),
        },
        new[]
        {
            Breakfast("الفطار: شوفان باللبن", "Breakfast: oats with milk", Oats.Of(50), Milk.Of(250), Banana.Of(1)),
            Snack("سناك: بيض وخيار", "Snack: eggs & cucumber", Egg.Of(2), Cucumber.Of(100)),
            Lunch("الغدا: سمك وبطاطس", "Lunch: fish & potatoes", Fish.Of(250), Potato.Of(200), Salad.Of(200)),
            Snack("سناك: زبادي", "Snack: yogurt", Yogurt.Of(170)),
            Dinner("العشا: تونة وسلطة", "Dinner: tuna & salad", Tuna.Of(120), Salad.Of(200), Bread.Of(0.5m)),
        },
        new[]
        {
            Breakfast("بديل الفطار: قريش وعيش", "Swap: cottage cheese & bread", Cottage.Of(150), Bread.Of(0.5m), Salad.Of(100)),
            Snack("بديل السناك: موزة", "Swap: banana", Banana.Of(1)),
            Lunch("بديل الغدا: كفتة ورز", "Swap: kofta & rice", Kofta.Of(150), Rice.Of(120), Salad.Of(200)),
            Snack("بديل السناك: زبادي", "Swap: yogurt", Yogurt.Of(170)),
            Dinner("بديل العشا: ٣ بيضات وسلطة", "Swap: 3 eggs & salad", Egg.Of(3), Salad.Of(150)),
        });

    private static readonly ReadyDiet Balanced = new("balanced_egyptian",
        new DietText("أكل مصري متوازن – ثبات الوزن", "Balanced Egyptian – keep your weight",
            "أكل البيت المعتاد بكميات مظبوطة: فول، رز، ملوخية، كفتة.",
            "Usual home food in the right amounts: ful, rice, molokhia, kofta."),
        DietGoal.Maintain,
        new[]
        {
            Breakfast("الفطار: فول وبيضة", "Breakfast: ful & an egg", Ful.Of(200), Egg.Of(1), Bread.Of(1), Salad.Of(100)),
            Snack("سناك: موزة ولبن", "Snack: banana & milk", Banana.Of(1), Milk.Of(200)),
            Lunch("الغدا: فراخ ورز وملوخية", "Lunch: chicken, rice & molokhia", Chicken.Of(150), Rice.Of(200), Molokhia.Of(250)),
            Snack("سناك: زبادي بالعسل", "Snack: yogurt with honey", Yogurt.Of(170), Honey.Of(10)),
            Dinner("العشا: جبنة بيضا وعيش", "Dinner: white cheese & bread", WhiteCheese.Of(50), Bread.Of(1), Cucumber.Of(100)),
        },
        new[]
        {
            Breakfast("الفطار: شوفان وزبدة فول سوداني", "Breakfast: oats & peanut butter", Oats.Of(60), Milk.Of(250), Banana.Of(1), PeanutButter.Of(15)),
            Snack("سناك: برتقال ولوز", "Snack: orange & almonds", Orange.Of(1), Almonds.Of(15)),
            Lunch("الغدا: كفتة ومكرونة", "Lunch: kofta & pasta", Kofta.Of(150), Pasta.Of(200), Salad.Of(150)),
            Snack("سناك: زبادي", "Snack: yogurt", Yogurt.Of(170)),
            Dinner("العشا: بيض وعيش", "Dinner: eggs & bread", Egg.Of(2), Bread.Of(1), Salad.Of(150)),
        },
        new[]
        {
            Breakfast("بديل الفطار: طعمية وعيش", "Swap: ta'ameya & bread", Taameya.Of(75), Bread.Of(1), Salad.Of(100)),
            Snack("بديل السناك: ٤ بلحات", "Swap: 4 dates", Dates.Of(4)),
            Lunch("بديل الغدا: سمك ورز", "Swap: fish & rice", Fish.Of(200), Rice.Of(200), Salad.Of(150)),
            Snack("بديل السناك: حمص وخيار", "Swap: hummus & cucumber", Hummus.Of(60), Cucumber.Of(100)),
            Dinner("بديل العشا: قريش وعيش", "Swap: cottage cheese & bread", Cottage.Of(200), Bread.Of(1)),
        });

    private static readonly ReadyDiet MuscleGain = new("muscle_gain_home",
        new DietText("زيادة عضل – أكل بيتي", "Muscle gain – home food",
            "سعرات وبروتين أكتر من أكل البيت: بيض، فراخ، كبدة، تونة، رز ومكرونة.",
            "More calories and protein from home food: eggs, chicken, liver, tuna, rice and pasta."),
        DietGoal.MuscleGain,
        new[]
        {
            Breakfast("الفطار: فول و٣ بيضات", "Breakfast: ful & 3 eggs", Ful.Of(200), Egg.Of(3), Bread.Of(1)),
            Snack("سناك: شوفان باللبن", "Snack: oats with milk", Oats.Of(60), Milk.Of(300), Banana.Of(1)),
            Lunch("الغدا: فراخ ورز", "Lunch: chicken & rice", Chicken.Of(200), Rice.Of(250), Salad.Of(150)),
            Snack("سناك: زبادي وعسل ولوز", "Snack: yogurt, honey & almonds", Yogurt.Of(200), Honey.Of(15), Almonds.Of(20)),
            Dinner("العشا: تونة ومكرونة", "Dinner: tuna & pasta", Tuna.Of(150), Pasta.Of(200), Salad.Of(100)),
        },
        new[]
        {
            Breakfast("الفطار: ٤ بيضات وجبنة", "Breakfast: 4 eggs & cheese", Egg.Of(4), WhiteCheese.Of(50), Bread.Of(1.5m)),
            Snack("سناك: عيش بزبدة فول سوداني", "Snack: bread with peanut butter", PeanutButter.Of(30), Bread.Of(1), Banana.Of(1)),
            Lunch("الغدا: كفتة ورز وملوخية", "Lunch: kofta, rice & molokhia", Kofta.Of(200), Rice.Of(250), Molokhia.Of(250)),
            Snack("سناك: لبن وبلح", "Snack: milk & dates", Milk.Of(300), Dates.Of(4)),
            Dinner("العشا: كبدة وعيش", "Dinner: liver & bread", Liver.Of(200), Bread.Of(1), Salad.Of(150)),
        },
        new[]
        {
            Breakfast("بديل الفطار: شوفان وزبدة فول سوداني", "Swap: oats & peanut butter", Oats.Of(80), Milk.Of(300), PeanutButter.Of(20)),
            Snack("بديل السناك: زبادي وبلح", "Swap: yogurt & dates", Yogurt.Of(200), Dates.Of(3)),
            Lunch("بديل الغدا: سمك ومكرونة", "Swap: fish & pasta", Fish.Of(250), Pasta.Of(250), Salad.Of(150)),
            Snack("بديل السناك: حمص وعيش", "Swap: hummus & bread", Hummus.Of(80), Bread.Of(1)),
            Dinner("بديل العشا: قريش وعيش وعسل", "Swap: cottage cheese, bread & honey", Cottage.Of(250), Bread.Of(1), Honey.Of(15)),
        });

    private static readonly ReadyDiet Ramadan = new("ramadan",
        new DietText("رمضان – فطار وسحور", "Ramadan – iftar & suhoor",
            "فطار على بلح وشوربة، وجبة رئيسية، سناك بعد التراويح، وسحور يشبّع لحد المغرب.",
            "Break the fast with dates and soup, a main meal, a snack after taraweeh and a filling suhoor."),
        null,
        new[]
        {
            Lunch("الفطار: بلح وشوربة عدس", "Iftar: dates & lentil soup", Dates.Of(3), Lentils.Of(250)),
            Dinner("الوجبة الرئيسية: فراخ ورز", "Main meal: chicken & rice", Chicken.Of(180), Rice.Of(180), Salad.Of(200)),
            Snack("بعد التراويح: زبادي وموزة", "After taraweeh: yogurt & banana", Yogurt.Of(170), Banana.Of(1)),
            Breakfast("السحور: فول وبيض", "Suhoor: ful & eggs", Ful.Of(200), Egg.Of(2), Bread.Of(1), Cucumber.Of(100)),
        },
        new[]
        {
            Lunch("الفطار: بلح ولبن", "Iftar: dates & milk", Dates.Of(3), Milk.Of(250)),
            Dinner("الوجبة الرئيسية: كفتة وبطاطس", "Main meal: kofta & potatoes", Kofta.Of(150), Potato.Of(200), Salad.Of(200)),
            Snack("بعد التراويح: برتقال ولوز", "After taraweeh: orange & almonds", Orange.Of(1), Almonds.Of(15)),
            Breakfast("السحور: قريش وبيض", "Suhoor: cottage cheese & eggs", Cottage.Of(200), Egg.Of(2), Bread.Of(1), Cucumber.Of(100)),
        },
        new[]
        {
            Lunch("بديل الفطار: برتقال وشوربة", "Swap: orange & soup", Orange.Of(1), Lentils.Of(250)),
            Dinner("بديل الرئيسية: سمك ورز", "Swap: fish & rice", Fish.Of(220), Rice.Of(180), Salad.Of(200)),
            Snack("بديل التراويح: زبادي بالعسل", "Swap: yogurt with honey", Yogurt.Of(170), Honey.Of(10)),
            Breakfast("بديل السحور: فول وزبادي", "Swap: ful & yogurt", Ful.Of(200), Bread.Of(1), Yogurt.Of(170)),
        });

    private static readonly ReadyDiet Budget = new("budget_ful_lentils_eggs",
        new DietText("اقتصادي – فول وعدس وبيض", "Budget – ful, lentils & eggs",
            "أرخص بروتين في مصر: فول وعدس وبيض وطعمية، بكميات محسوبة.",
            "Egypt's cheapest protein: ful, lentils, eggs and ta'ameya, in measured amounts."),
        null,
        new[]
        {
            Breakfast("الفطار: فول وعيش", "Breakfast: ful & bread", Ful.Of(200), Bread.Of(1), Salad.Of(100)),
            Snack("سناك: موزة", "Snack: banana", Banana.Of(1)),
            Lunch("الغدا: شوربة عدس وبيض", "Lunch: lentil soup & eggs", Lentils.Of(400), Bread.Of(1), Egg.Of(2)),
            Snack("سناك: ٣ بلحات", "Snack: 3 dates", Dates.Of(3)),
            Dinner("العشا: بيض وعيش", "Dinner: eggs & bread", Egg.Of(2), Bread.Of(1), Cucumber.Of(150)),
        },
        new[]
        {
            Breakfast("الفطار: طعمية وعيش", "Breakfast: ta'ameya & bread", Taameya.Of(75), Bread.Of(1), Salad.Of(100)),
            Snack("سناك: برتقال", "Snack: orange", Orange.Of(1)),
            Lunch("الغدا: كشري وسلطة", "Lunch: koshari & salad", Koshari.Of(350), Salad.Of(150)),
            Snack("سناك: لبن", "Snack: milk", Milk.Of(250)),
            Dinner("العشا: فول وبيضة", "Dinner: ful & an egg", Ful.Of(150), Egg.Of(1), Bread.Of(0.5m)),
        },
        new[]
        {
            Breakfast("بديل الفطار: بيض وعيش", "Swap: eggs & bread", Egg.Of(2), Bread.Of(1), Cucumber.Of(100)),
            Snack("بديل السناك: بلح", "Swap: dates", Dates.Of(3)),
            Lunch("بديل الغدا: كبدة ورز", "Swap: liver & rice", Liver.Of(150), Rice.Of(150), Salad.Of(150)),
            Snack("بديل السناك: لبن", "Swap: milk", Milk.Of(250)),
            Dinner("بديل العشا: قريش وعيش", "Swap: cottage cheese & bread", Cottage.Of(150), Bread.Of(1)),
        });

    public static IReadOnlyList<ReadyDiet> All { get; } = new[] { FatLoss, Balanced, MuscleGain, Ramadan, Budget };

    public static ReadyDiet? Find(string key) =>
        All.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Diets for this goal first, then the ones that fit any goal, then the rest.</summary>
    public static IReadOnlyList<ReadyDiet> ForGoal(DietGoal? goal) =>
        All.OrderBy(d => d.Goal == goal ? 0 : d.Goal is null ? 1 : 2).ToList();
}
