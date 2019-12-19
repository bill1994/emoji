using UnityEngine;
using System.Collections;

namespace Kyub
{
    public class PersistentObject : MonoBehaviour
    {
        protected virtual void Awake()
        {
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }
    }
}
