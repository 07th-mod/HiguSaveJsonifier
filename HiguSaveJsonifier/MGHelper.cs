public static class MGHelper {
	private static readonly byte[] Key = {
		229,
		99,
		174,
		4,
		45,
		166,
		127,
		158,
		69
	};

	public static void KeyEncode(byte[] b)
	{
		byte[] array = (byte[])Key.Clone();
		for (int i = 0; i < b.Length; i++) {
			b[i] = (byte)(b[i] ^ array[i % Key.Length]);
			array[i % array.Length] += 27;
		}
	}
}