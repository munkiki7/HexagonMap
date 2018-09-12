using UnityEngine;

public struct PseudoRandom
{
	public float existingChance, houseRotation, housePick;
	public int treeType;

	public static PseudoRandom Create()
	{
		PseudoRandom random;
		random.existingChance = Random.value * 0.999f;
		random.houseRotation = Random.value * 0.999f;
		random.housePick = Random.value * 0.999f;
		random.treeType = Random.Range(0, 3);
		return random;
	}
}
