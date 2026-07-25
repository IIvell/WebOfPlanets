using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Crafting kao ne-UI servis: otkrivanje recepata, mapiranje rezultata i
    // transakcija craftanja. Ranije je sve troje živjelo u CraftingUI, pa
    // craftanje nije postojalo bez panela — obrazac je sada isti kao kod
    // HubProgress/ConnectionManager (UI delegira u domensku klasu).
    public static class CraftingSystem
    {
        // Recepti se auto-otkrivaju iz Resources/Recipes — novi recept asset ne
        // treba ručno dodavati u scene listu (scene lista ostaje podržana zbog
        // redoslijeda prikaza).
        public static CraftingRecipe[] MergeWithResources(CraftingRecipe[] sceneRecipes)
        {
            CraftingRecipe[] loaded = Resources.LoadAll<CraftingRecipe>("Recipes");
            if (loaded == null || loaded.Length == 0) return sceneRecipes;

            var merged = new List<CraftingRecipe>();
            if (sceneRecipes != null)
                foreach (var r in sceneRecipes)
                    if (r != null && !merged.Contains(r))
                        merged.Add(r);
            foreach (var r in loaded)
                if (!merged.Contains(r))
                    merged.Add(r);
            return merged.ToArray();
        }

        // Rezultat recepta kao hotbar item — mapiranje typed result polja
        // (vidi S2 u auditu: puna konsolidacija tih polja traži migraciju
        // recipe asseta, pa dispatch ostaje, ali na jednom mjestu).
        public static QuickSlotItem GetResultItem(CraftingRecipe recipe) => recipe.resultType switch
        {
            CraftingRecipe.ResultType.Tool             => recipe.resultTool,
            CraftingRecipe.ResultType.CollectorMachine => recipe.resultMachine,
            CraftingRecipe.ResultType.StorageMachine   => recipe.resultStorageMachine,
            CraftingRecipe.ResultType.SmelterMachine   => recipe.resultSmelterMachine,
            CraftingRecipe.ResultType.ExtractorMachine => recipe.resultExtractorMachine,
            CraftingRecipe.ResultType.UplinkMachine    => recipe.resultUplinkMachine,
            CraftingRecipe.ResultType.TeleporterMachine => recipe.resultTeleporterMachine,
            CraftingRecipe.ResultType.TwoWayTeleporterMachine => recipe.resultTwoWayTeleporterMachine,
            CraftingRecipe.ResultType.NetworkMapDevice => recipe.resultNetworkMapDevice,
            CraftingRecipe.ResultType.RespawnTotem     => recipe.resultRespawnTotem,
            CraftingRecipe.ResultType.GasMask          => recipe.resultGasMask,
            CraftingRecipe.ResultType.Computer         => recipe.resultComputer,
            _                                          => null
        };

        // Transakcija craftanja: PRVO rezultat u hotbar, pa tek onda potrošnja
        // sastojaka — da se sastojci ne izgube kad je hotbar pun. TestingMode
        // crafta besplatno i bez otključanog praga. Vraća false ako rezultat
        // nije stao u hotbar (ili recept nije dostupan).
        public static bool TryCraft(CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            if (!GameManager.TestingMode && (!recipe.IsUnlocked || !recipe.CanAfford())) return false;

            QuickSlotItem result = GetResultItem(recipe);
            if (result == null) return false;
            if (QuickSlotInventory.Instance == null || !QuickSlotInventory.Instance.TryAdd(result, out _))
                return false;

            if (!GameManager.TestingMode)
                recipe.ConsumeIngredients();
            return true;
        }
    }
}
