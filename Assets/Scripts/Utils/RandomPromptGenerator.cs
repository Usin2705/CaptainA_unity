using System.Collections.Generic;
using UnityEngine;
public class RandomPromptGenerator : MonoBehaviour
{
    private List<string> roomTypes = new List<string> { "living room"}; //, "bedroom", "study", "dining room" };
    private List<string> positions = new List<string> { "left", "right", "center", "far left", "far right" };
    private List<string> colors = new List<string> { "red", "blue", "green", "yellow", "white", "black", "pink", "orange", "grey"};
    private List<string> furnitureItems = new List<string> { "sofa", "armchair", "coffee table", "bookshelf", "cupboard", "wardrobe", "desk", "chair" };
    private List<string> electronicOrDecorativeItems = new List<string> { "TV", "lamp", "clock", "radio"};
    private List<string> doorOrWindowItems = new List<string> { "door", "window", "balcony door" };
    private List<string> decorativeOrFunctionalItems = new List<string> { "picture", "photo", "mug", "plate", "book", "newspaper", "vase with flowers" };
    private List<string> animalOrClothingItems = new List<string> { "cat", "dog", "mitten", "t-shirt", "pants", "knitted hat", "shoes", "pair of boots" };
    private List<string> wallItems = new List<string> { "clock", "picture", "photo", "mirror", "coat hanger"};
    public string GeneratePrompt()
    {
        System.Random rnd = new System.Random();

        string prompt = "Illustration of a spacious " + roomTypes[rnd.Next(roomTypes.Count)] + " with a focus on simplicity and clarity. ";
        prompt += "On the " + positions[rnd.Next(positions.Count)] + ", there's a " + colors[rnd.Next(colors.Count)] + " " + furnitureItems[rnd.Next(furnitureItems.Count)] + ". ";
        prompt += "Next to it stands a " + colors[rnd.Next(colors.Count)] + " " + furnitureItems[rnd.Next(furnitureItems.Count)] + ". ";
        prompt += "In front of the " + furnitureItems[rnd.Next(furnitureItems.Count)] + " is a " + colors[rnd.Next(colors.Count)] + " " + furnitureItems[rnd.Next(furnitureItems.Count)] + ". ";
        prompt += "To the " + positions[rnd.Next(positions.Count)] + ", a " + colors[rnd.Next(colors.Count)] + " " + electronicOrDecorativeItems[rnd.Next(electronicOrDecorativeItems.Count)] + " is placed. ";
        prompt += "Beside it is a " + colors[rnd.Next(colors.Count)] + " " + furnitureItems[rnd.Next(furnitureItems.Count)] + ". ";
        prompt += "A " + colors[rnd.Next(colors.Count)] + " " + doorOrWindowItems[rnd.Next(doorOrWindowItems.Count)] + " is on the " + positions[rnd.Next(positions.Count)] + ". ";
        prompt += "On the floor below the " + doorOrWindowItems[rnd.Next(doorOrWindowItems.Count)] + ", there's a " + colors[rnd.Next(colors.Count)] + " mattress. ";

        int numberOfWallItems = rnd.Next(2, 4); // Randomly choose either 2 or 3
        List<string> selectedWallItems = new List<string>();
        for (int i = 0; i < numberOfWallItems; i++)
        {
            string wallItem;
            do
            {
                wallItem = wallItems[rnd.Next(wallItems.Count)];
            } while (selectedWallItems.Contains(wallItem)); // Ensure different wall items are selected

            selectedWallItems.Add(wallItem);
            prompt += "On the wall, there's a " + colors[rnd.Next(colors.Count)] + " " + wallItem + ". ";
        }

        prompt += "Scattered around the room, there's a " + colors[rnd.Next(colors.Count)] + " " + decorativeOrFunctionalItems[rnd.Next(decorativeOrFunctionalItems.Count)] + ", ";
        prompt += "a " + colors[rnd.Next(colors.Count)] + " " + animalOrClothingItems[rnd.Next(animalOrClothingItems.Count)] + ", ";
        prompt += "and a " + colors[rnd.Next(colors.Count)] + " " + decorativeOrFunctionalItems[rnd.Next(decorativeOrFunctionalItems.Count)] + ".";

        return prompt;
    }
}