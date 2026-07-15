using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Testing.Models
{
    /// <summary>
    /// Tentity (or Test Entity) is a basic models with types properties to be used for testing.
    /// </summary>
    /// <typeparam name="T">Type of the custom property</typeparam>
    public class Tentity<T>
    {
        // Fields
        public int number;
        public long bigNumber;
        public float floatingNumber;
        public double bigFloatingNumber;
        public string text;
        public bool boolean;
        public T custom;
        public int[] numbersArray;
        public List<int> numbersList;
        public long[] bigNumbersArray;
        public List<long> bigNumbersList;
        public float[] floatingNumbersArray;
        public List<float> floatingNumbersList;
        public double[] bigFloatingNumbersArray;
        public List<double> bigFloatingNumbersList;
        public string[] textsArray;
        public List<string> textsList;
        public bool[] booleanArray;
        public List<bool> booleanList;
        public T[] customArray;
        public List<T> customList;

        //Properties
        public int Number { get => number; set => number = value; }
        public long BigNumber { get => bigNumber; set => bigNumber = value; }
        public float FloatingNumber { get => floatingNumber; set => floatingNumber = value; }
        public double BigFloatingNumber { get => bigFloatingNumber; set => bigFloatingNumber = value; }
        public string Text { get => text; set => text = value; }
        public bool Boolean { get => boolean; set => boolean = value; }
        public T Custom { get => custom; set => custom = value; }
        public int[] NumbersArray { get => numbersArray; set => numbersArray = value; }
        public List<int> NumbersList { get => numbersList; set => numbersList = value; }
        public long[] BigNumbersArray { get => bigNumbersArray; set => bigNumbersArray = value; }
        public List<long> BigNumbersList { get => bigNumbersList; set => bigNumbersList = value; }
        public float[] FloatingNumbersArray { get => floatingNumbersArray; set => floatingNumbersArray = value; }
        public List<float> FloatingNumbersList { get => floatingNumbersList; set => floatingNumbersList = value; }
        public double[] BigFloatingNumbersArray { get => bigFloatingNumbersArray; set => bigFloatingNumbersArray = value; }
        public List<double> BigFloatingNumbersList { get => bigFloatingNumbersList; set => bigFloatingNumbersList = value; }
        public string[] TextsArray { get => textsArray; set => textsArray = value; }
        public List<string> TextsList { get => textsList; set => textsList = value; }
        public bool[] BooleanArray { get => booleanArray; set => booleanArray = value; }
        public List<bool> BooleanList { get => booleanList; set => booleanList = value; }
        public T[] CustomArray { get => customArray; set => customArray = value; }
        public List<T> CustomList { get => customList; set => customList = value; }

    }

    /// <inheritdoc cref="Tentity{T}"/>
    public class Tentity : Tentity<Null>
    {
    }
}
