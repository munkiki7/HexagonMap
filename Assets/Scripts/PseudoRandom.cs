using UnityEngine;

public struct PseudoRandom
{
	public float existingChance, rotation, housePick;
	public int treeType;

	public static PseudoRandom Create()
	{
		PseudoRandom random;
		random.existingChance = Random.value * 0.999f;
		random.rotation = Random.value * 0.999f;
		random.housePick = Random.value * 0.999f;
		random.treeType = Random.Range(0, 3);
		return random;
	}
}
