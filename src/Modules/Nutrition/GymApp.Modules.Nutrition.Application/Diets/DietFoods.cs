namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>Energy and macros of <see cref="DietServing.Per"/> units of a food.</summary>
internal sealed record DietMacros(decimal Calories, decimal Protein, decimal Carbs, decimal Fat);

/// <summary>
/// How a food is measured: its <see cref="Unit"/> (g, ml or pc), the amount its macros are given
/// for, and the <see cref="Step"/> a scaled portion is rounded to (10 g, 50 ml, a whole egg, a
/// quarter loaf).
/// </summary>
internal sealed record DietServing(string Unit, decimal Per, decimal Step);

/// <summary>A food in the ready diets (values from the Egyptian food database seed where it has the food).</summary>
internal sealed record DietFood(string NameAr, string NameEn, DietServing Serving, DietMacros Macros)
{
    public DietPortion Of(decimal amount) => new(this, amount);

    /// <summary>The macros of <paramref name="amount"/> units.</summary>
    public DietMacros For(decimal amount)
    {
        var share = amount / Serving.Per;
        return new DietMacros(Macros.Calories * share, Macros.Protein * share, Macros.Carbs * share, Macros.Fat * share);
    }
}

/// <summary>An amount of a food in a meal.</summary>
internal sealed record DietPortion(DietFood Food, decimal Amount);

/// <summary>Egyptian home food the ready diets are made of.</summary>
internal static class DietFoods
{
    private const string G = "g";
    private const string Ml = "ml";
    private const string Piece = "pc";

    private static DietMacros M(decimal kcal, decimal protein, decimal carbs, decimal fat) => new(kcal, protein, carbs, fat);

    private static DietFood Grams(string ar, string en, DietMacros per100) => new(ar, en, new DietServing(G, 100, 10), per100);

    private static DietFood Pieces(string ar, string en, decimal step, DietMacros each) => new(ar, en, new DietServing(Piece, 1, step), each);

    private static DietFood Millilitres(string ar, string en, DietMacros per100) => new(ar, en, new DietServing(Ml, 100, 50), per100);

    public static readonly DietFood Ful = Grams("فول مدمس", "Ful medames", M(94, 6.5m, 15, 0.5m));
    public static readonly DietFood Egg = Pieces("بيض مسلوق", "Boiled egg", 1, M(78, 6, 0.5m, 5));
    public static readonly DietFood Bread = Pieces("عيش بلدي", "Baladi bread (loaf)", 0.25m, M(210, 7, 42, 1));
    public static readonly DietFood Chicken = Grams("فراخ مشوية", "Grilled chicken", M(154, 23.3m, 0, 6.7m));
    public static readonly DietFood Rice = Grams("رز مطبوخ", "Cooked rice", M(130, 2.7m, 28.7m, 0.3m));
    public static readonly DietFood Pasta = Grams("مكرونة مسلوقة", "Boiled pasta", M(131, 4.5m, 25, 1));
    public static readonly DietFood Potato = Grams("بطاطس مسلوقة", "Boiled potato", M(77, 2, 18, 0.1m));
    public static readonly DietFood SweetPotato = Grams("بطاطا مشوية", "Baked sweet potato", M(90, 1.3m, 20.7m, 0.1m));
    public static readonly DietFood Salad = Grams("سلطة خضرا", "Green salad", M(20, 1.3m, 3.3m, 0.2m));
    public static readonly DietFood Cucumber = Grams("خيار", "Cucumber", M(15, 1, 3, 0.1m));
    public static readonly DietFood Yogurt = Grams("زبادي يوناني", "Greek yogurt", M(97, 13.3m, 4, 2.7m));
    public static readonly DietFood Cottage = Grams("جبنة قريش", "Cottage cheese", M(98, 11, 3.4m, 4.3m));
    public static readonly DietFood WhiteCheese = Grams("جبنة بيضا", "White cheese", M(250, 13.3m, 3.3m, 20));
    public static readonly DietFood Tuna = Grams("تونة مصفّاة", "Tuna in water", M(116, 26, 0, 1));
    public static readonly DietFood Fish = Grams("سمك بلطي مشوي", "Grilled tilapia", M(108, 22.7m, 0, 2));
    public static readonly DietFood Kofta = Grams("كفتة مشوية", "Grilled kofta", M(193, 16.7m, 2, 13.3m));
    public static readonly DietFood Liver = Grams("كبدة", "Liver (kebda)", M(149, 20, 3.3m, 6));
    public static readonly DietFood Molokhia = Grams("ملوخية", "Molokhia", M(34, 2, 4, 1.2m));
    public static readonly DietFood Lentils = Millilitres("شوربة عدس", "Lentil soup", M(60, 4, 10, 0.3m));
    public static readonly DietFood Milk = Millilitres("لبن", "Milk", M(60, 3.2m, 4.8m, 3.2m));
    public static readonly DietFood Oats = Grams("شوفان", "Oats", M(380, 12.5m, 67.5m, 7.5m));
    public static readonly DietFood Hummus = Grams("حمص بالطحينة", "Hummus", M(166, 8, 14, 10));
    public static readonly DietFood Koshari = Grams("كشري", "Koshari", M(117, 4, 21.7m, 1.7m));
    public static readonly DietFood Taameya = Grams("طعمية", "Ta'ameya", M(333, 13, 31, 18));
    public static readonly DietFood Banana = Pieces("موز", "Banana", 1, M(107, 1, 27, 0.4m));
    public static readonly DietFood Orange = Pieces("برتقال", "Orange", 1, M(70, 1, 18, 0.2m));
    public static readonly DietFood Dates = Pieces("بلح", "Date", 1, M(22, 0.2m, 6, 0));
    public static readonly DietFood PeanutButter = Grams("زبدة فول سوداني", "Peanut butter", M(594, 22, 22, 50));
    public static readonly DietFood Almonds = Grams("لوز", "Almonds", M(577, 20, 20, 50));
    public static readonly DietFood Honey = Grams("عسل نحل", "Honey", M(304, 0.3m, 81, 0));
}
