using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    public enum WeaponType { Shovel, Knife, Hammer }

    [System.Serializable]
    public struct WeaponStats
    {
        public int   damage;
        public float radius;
        public float rpm;
        public bool  clockwise;
        public float enemyKnockback;
    }

    [Header("Silah Prefabları")]
    [SerializeField] GameObject shovelPrefab;
    [SerializeField] GameObject knifePrefab;
    [SerializeField] GameObject hammerPrefab;

    [Header("Başlangıç Loadout")]
    [SerializeField] WeaponType[] startingWeapons = new WeaponType[] { WeaponType.Shovel };

    [Header("Ammo ile Çoğaltılacak Tip")]
    [SerializeField] WeaponType ammoWeaponType = WeaponType.Shovel; // ★ HANGİ TİPİ çoğaltalım?

    [Header("Stat Tablosu (0=Shovel,1=Knife,2=Hammer)")]
    [SerializeField] WeaponStats[] typeStats = new WeaponStats[3];

    public WeaponStats GetStats(WeaponType t)
    {
        int i = (int)t;
        return (typeStats != null && i >= 0 && i < typeStats.Length) ? typeStats[i] : default;
    }

    void Start()
    {
        // Başlangıç silahlarını ver
        foreach (var t in startingWeapons)
        {
            var prefab = GetPrefab(t);
            if (!prefab) continue;

            var go = Instantiate(prefab, transform);
            var orbit = go.GetComponent<WeaponOrbit>();
            if (orbit)
            {
                orbit.SetType(t);
                orbit.RefreshFromLoadout(this);
            }
        }

        foreach (var orbit in GetComponentsInChildren<WeaponOrbit>())
            orbit.RefreshFromLoadout(this);
    }

    GameObject GetPrefab(WeaponType t) => t switch
    {
        WeaponType.Shovel => shovelPrefab,
        WeaponType.Knife  => knifePrefab,
        WeaponType.Hammer => hammerPrefab,
        _ => null
    };

    public void PushStatsToChildren()
    {
        foreach (var orbit in GetComponentsInChildren<WeaponOrbit>())
            orbit.RefreshFromLoadout(this);
    }

    // ===================== EŞİT AÇILI ÇOĞALTMA =====================

    public void SetWeaponCountForType(WeaponType t, int count)
    {
        if (count < 1) count = 1;

        System.Collections.Generic.List<WeaponOrbit> list = new System.Collections.Generic.List<WeaponOrbit>();
        foreach (var orbit in GetComponentsInChildren<WeaponOrbit>(true))
            if (orbit && orbit.type == t) list.Add(orbit);

        while (list.Count < count)
        {
            var prefab = GetPrefab(t);
            if (!prefab)
            {
                Debug.LogWarning($"[PlayerLoadout] Prefab bulunamadı: {t}");
                break;
            }

            var go = Instantiate(prefab, transform);
            var orbit = go.GetComponent<WeaponOrbit>();
            if (orbit)
            {
                orbit.SetType(t);
                orbit.RefreshFromLoadout(this);
                list.Add(orbit);
            }
        }

        while (list.Count > count)
        {
            var last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            if (last) Destroy(last.gameObject);
        }

        float step = 360f / count;
        for (int i = 0; i < list.Count; i++)
        {
            var orbit = list[i];
            if (!orbit) continue;
            orbit.RefreshFromLoadout(this);
            orbit.SetAngleDeg(i * step);
        }
    }

    /// <summary>
    /// ★ Ammo tarafı burada sabit bir tipten çoğaltır (ammoWeaponType).
    /// </summary>
    public void SetOrbitingCountByFirstType(int totalCount)
    {
        SetWeaponCountForType(ammoWeaponType, totalCount);
    }
}
