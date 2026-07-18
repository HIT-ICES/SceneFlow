using UnityEngine;

public class Util : MonoBehaviour
{
    public static Vector3 formatMoney(int money)
    {
        var x = money / 10000;
        var y = (money - x * 10000) / 100;
        var z = money - x * 10000 - y * 100;

        return new Vector3(x, y, z);
    }
}