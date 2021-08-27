using RimWorld;
using System.Linq;

namespace Verse
{
	public class RecipeWorkerCounter_ExtractSeeds : RecipeWorkerCounter
	{
		public override bool CanCountProducts(Bill_Production bill)
		{
			if (bill.ingredientFilter.AllowedThingDefs.Count<ThingDef>() != 1) return false;
            return true;
		}

		public override int CountProducts(Bill_Production bill)
		{
            if (bill.ingredientFilter.AllowedThingDefs.Count<ThingDef>() != 1) return 0;
			return bill.Map.resourceCounter.GetCount(bill.ingredientFilter.AllowedThingDefs.First<ThingDef>().butcherProducts[0].thingDef);
		}

		public override string ProductsDescription(Bill_Production bill)
		{
            if (bill.ingredientFilter.AllowedThingDefs.Count<ThingDef>() != 1) return "Invalid";
			return bill.ingredientFilter.AllowedThingDefs.First<ThingDef>().butcherProducts[0].thingDef.label;
		}

		public override bool CanPossiblyStoreInStockpile(Bill_Production bill, Zone_Stockpile stockpile)
		{
            if (bill.ingredientFilter.AllowedThingDefs.Count<ThingDef>() != 1) return false;
			if (!stockpile.GetStoreSettings().AllowedToAccept(bill.ingredientFilter.AllowedThingDefs.First<ThingDef>()))
            {
                return false;
            }
			return true;
		}
	}
}
