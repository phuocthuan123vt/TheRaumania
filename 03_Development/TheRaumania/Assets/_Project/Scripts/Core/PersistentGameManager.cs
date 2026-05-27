using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PersistentGameManager : MonoBehaviour
{
    public static PersistentGameManager Instance;

    [Header("Scene Switching")]
    [SerializeField] private bool disableNavMeshPlaneRenderer = true;

    // Lưu lại điểm sẽ xuất hiện sau khi chuyển Scene
    public static string TargetSpawnPointName = "";

    // Gom Player, Main Camera, HUD Canvas, Managers vào làm con (Child) của Script này
    // Để giữ nguyên trạng thái bay từ Scene sang Scene khác
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure persistent root is undestroyable; call on root to avoid editor warnings
            DontDestroyOnLoad(this.transform.root.gameObject); // Bất tử qua mọi bản map
            
            // Đăng ký rà soát sự kiện khi 1 Map mới được load xong
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            // Tránh bị nhân đôi Player / HUD nếu mội map đều có con Player có sẵn
            Destroy(gameObject); 
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure gameplay isn't stuck in pause after scene transitions
        Time.timeScale = 1f;
        // Khi Scene được load lên (scn_Restaurant, scn_Store)
        // Nếu có TargetSpawnPointName, cố di chuyển Player tới điểm đó
        if (!string.IsNullOrEmpty(TargetSpawnPointName))
        {
            MovePlayerToSpawnPoint(TargetSpawnPointName);
            TargetSpawnPointName = ""; // Làm trống cho lần sau
        }
        // Sau khi load xong scene, đảm bảo chỉ có một active EventSystem
        EnsureSingleEventSystem();
        // Cập nhật trạng thái Managers và hiển thị renderer cho scene mới
        UpdateManagersForActiveScene(scene);
        UpdateActiveSceneVisuals(scene);
        // Map UI panels from the scene into persistent managers (so CookingUIManager gets its panels)
        MapSceneUIToManagers(scene);
        // Wire storage/kitchen interactables if their UnityEvent targets were lost
        WireSceneInteractables(scene);
    }

    // Luôn giữ Managers trong PersistentGameManager hoạt động; tắt Managers scene-local để tránh duplicate singletons
    private void UpdateManagersForActiveScene(Scene activeScene)
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.IsValid()) continue;
            var roots = s.GetRootGameObjects();
            foreach (var go in roots)
            {
                if (go.name != "Managers") continue;

                // Managers dưới PersistentGameManager luôn bật
                if (go.transform.IsChildOf(this.transform))
                {
                    if (!go.activeSelf) go.SetActive(true);
                    continue;
                }

                // Tắt Managers scene-local để tránh ghi đè singleton (Hotbar, Inventory, v.v.)
                if (go.activeSelf)
                {
                    go.SetActive(false);
                    Debug.Log($"PersistentGameManager: disabled scene-local Managers in scene '{s.name}'.");
                }
            }
        }
    }

    // Thay đổi trạng thái hiển thị renderer cho các scene: chỉ hiện renderer của scene active,
    // ẩn renderer (SpriteRenderer, TilemapRenderer, MeshRenderer, Canvas) ở scene khác nhưng giữ GameObjects active
    private void UpdateActiveSceneVisuals(Scene activeScene)
    {
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.IsValid()) continue;
            bool visible = (s == activeScene);
            var roots = s.GetRootGameObjects();
            foreach (var root in roots)
            {
                // Never toggle visuals/colliders under our persistent root
                if (root.transform.IsChildOf(this.transform)) continue;

                // Toggling renderers under this root
                var spriteRenderers = root.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true);
                foreach (var r in spriteRenderers) r.enabled = visible;

                var meshRenderers = root.GetComponentsInChildren<UnityEngine.MeshRenderer>(true);
                foreach (var r in meshRenderers) r.enabled = visible;

                var tilemapRenderers = root.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true);
                foreach (var r in tilemapRenderers) r.enabled = visible;

                var canvases = root.GetComponentsInChildren<UnityEngine.Canvas>(true);
                foreach (var c in canvases) c.enabled = visible;

                var spriteMasks = root.GetComponentsInChildren<UnityEngine.SpriteMask>(true);
                foreach (var m in spriteMasks) m.enabled = visible;
                // Toggle physics colliders so player isn't blocked by non-active scenes
                var coll2Ds = root.GetComponentsInChildren<UnityEngine.Collider2D>(true);
                foreach (var col in coll2Ds) col.enabled = visible;

                var coll3Ds = root.GetComponentsInChildren<UnityEngine.Collider>(true);
                foreach (var col in coll3Ds) col.enabled = visible;

                var shadowCasters = root.GetComponentsInChildren<UnityEngine.Rendering.Universal.ShadowCaster2D>(true);
                foreach (var sc in shadowCasters) sc.enabled = visible;

                var lights2D = root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true);
                foreach (var l in lights2D) l.enabled = visible;
            }
            Debug.Log($"PersistentGameManager: Set scene '{s.name}' visuals visible={visible}.");
        }

        if (disableNavMeshPlaneRenderer && activeScene.IsValid())
        {
            DisableNavMeshPlaneRenderers(activeScene);
        }
    }

    private void DisableNavMeshPlaneRenderers(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var meshRenderers = root.GetComponentsInChildren<UnityEngine.MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                string n = mr.gameObject.name;
                if (n.Contains("Plane") || n.Contains("NavMesh") || n.Contains("Navmesh"))
                {
                    mr.enabled = false;
                }
            }
        }
    }

    // Attempts to map scene-local UI panels into persistent managers (e.g., CookingUIManager fields)
    private void MapSceneUIToManagers(Scene scene)
    {
        if (!scene.IsValid()) return;

        // Find persistent CookingUIManager instance under our root Managers
        var cookingUI = GetComponentInChildren<CookingUIManager>(true);
        if (cookingUI == null) return;

        // Helper to find a GameObject by name in the loaded scene (recursive)
        GameObject FindPanel(string name)
        {
            var rootObjs = scene.GetRootGameObjects();
            foreach (var root in rootObjs)
            {
                var found = FindInChildren(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        Transform FindInChildren(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }

        if (cookingUI.pnl_RecipeBook == null)
        {
            var pnl = FindPanel("pnl_RecipeBook");
            if (pnl != null)
            {
                cookingUI.pnl_RecipeBook = pnl;
                Debug.Log("PersistentGameManager: Mapped pnl_RecipeBook into CookingUIManager.");
            }
        }

        if (cookingUI.pnl_PrepMinigame == null)
        {
            var pnl = FindPanel("pnl_PrepMinigame");
            if (pnl != null)
            {
                cookingUI.pnl_PrepMinigame = pnl;
                Debug.Log("PersistentGameManager: Mapped pnl_PrepMinigame into CookingUIManager.");
            }
        }

        if (cookingUI.pnl_SlicingMinigame == null)
        {
            var pnl = FindPanel("pnl_SlicingMinigame");
            if (pnl != null)
            {
                cookingUI.pnl_SlicingMinigame = pnl;
                Debug.Log("PersistentGameManager: Mapped pnl_SlicingMinigame into CookingUIManager.");
            }
        }

        if (cookingUI.pnl_FryingMinigame == null)
        {
            var pnl = FindPanel("pnl_FryingMinigame");
            if (pnl != null)
            {
                cookingUI.pnl_FryingMinigame = pnl;
                Debug.Log("PersistentGameManager: Mapped pnl_FryingMinigame into CookingUIManager.");
            }
        }

        // Map upgrade dialog panel into UpgradeManager if present
        var upgradeMgr = GetComponentInChildren<UpgradeManager>(true);
        if (upgradeMgr != null && upgradeMgr.pnlUpgradeDialog == null)
        {
            var pnl = FindPanel("pnl_UpgradeDialog");
            if (pnl != null)
            {
                upgradeMgr.pnlUpgradeDialog = pnl;
                // Try to find common UI fields
                var txt = pnl.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (txt != null) upgradeMgr.txtUpgradeMessage = txt;
                var input = pnl.GetComponentInChildren<TMPro.TMP_InputField>(true);
                if (input != null) upgradeMgr.inputOfferAmount = input;
                var buttons = pnl.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                if (buttons != null && buttons.Length > 0)
                {
                    // Heuristic: first button = Agree, second = Bargain, third = Cancel
                    if (buttons.Length > 0) upgradeMgr.btnAgree = buttons[0];
                    if (buttons.Length > 1) upgradeMgr.btnBargain = buttons[1];
                    if (buttons.Length > 2) upgradeMgr.btnCancel = buttons[2];
                }
                Debug.Log("PersistentGameManager: Mapped pnl_UpgradeDialog into UpgradeManager.");
            }
        }
    }

    private void WireSceneInteractables(Scene scene)
    {
        if (!scene.IsValid()) return;

        var warehouseUI = GetComponentInChildren<WarehouseUI>(true);
        var cookingManager = CookingManager.Instance != null
            ? CookingManager.Instance
            : GetComponentInChildren<CookingManager>(true);
        var shopUI = GetComponentInChildren<ShopUI>(true);

        if (warehouseUI == null && cookingManager == null && shopUI == null) return;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var interactables = root.GetComponentsInChildren<Interactable>(true);
            foreach (var it in interactables)
            {
                if (!NeedsRuntimeListener(it)) continue;

                string name = it.gameObject.name;
                if (warehouseUI != null && name.Contains("ColdStorage"))
                {
                    it.onInteract.AddListener(warehouseUI.OpenColdStorage);
                    Debug.Log("PersistentGameManager: wired OpenColdStorage on " + name);
                }
                else if (warehouseUI != null && name.Contains("DryStorage"))
                {
                    it.onInteract.AddListener(warehouseUI.OpenDryStorage);
                    Debug.Log("PersistentGameManager: wired OpenDryStorage on " + name);
                }
                else if (cookingManager != null && name.Contains("Kitchen"))
                {
                    it.onInteract.AddListener(cookingManager.OpenRecipeBook);
                    Debug.Log("PersistentGameManager: wired OpenRecipeBook on " + name);
                }
                else if (shopUI != null && (name.Contains("TwoFinger") || name.Contains("Shop")))
                {
                    it.onInteract.AddListener(shopUI.ToggleShop);
                    Debug.Log("PersistentGameManager: wired ToggleShop on " + name);
                }
            }
        }
    }

    private bool NeedsRuntimeListener(Interactable it)
    {
        if (it == null || it.onInteract == null) return true;
        int count = it.onInteract.GetPersistentEventCount();
        if (count == 0) return true;
        for (int i = 0; i < count; i++)
        {
            var target = it.onInteract.GetPersistentTarget(i);
            if (target != null) return false;
        }
        return true;
    }

    // Public helper để di chuyển player tới spawn point (có thể gọi từ ScenePortal nếu scene đã được load)
    public void MovePlayerToSpawnPoint(string spawnPointName)
    {
        if (string.IsNullOrEmpty(spawnPointName)) return;
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint != null)
        {
            PlayerMovement player = GetComponentInChildren<PlayerMovement>(true);
            if (player != null)
            {
                player.transform.position = spawnPoint.transform.position;
                // Reset animation nếu cần
                Debug.Log($"PersistentGameManager: Moved player to spawn '{spawnPointName}'.");
            }
            else
            {
                Debug.LogWarning("PersistentGameManager: PlayerMovement not found as child when trying to move player.");
            }
        }
        else
        {
            Debug.LogWarning($"PersistentGameManager: spawn point '{spawnPointName}' not found in scene.");
        }
    }

    // Public helper để kích hoạt scene (bật Managers của scene đó và cập nhật hiển thị)
    public void ActivateSceneForPlayer(Scene scene)
    {
        if (!scene.IsValid()) return;
        Time.timeScale = 1f;
        UpdateManagersForActiveScene(scene);
        UpdateActiveSceneVisuals(scene);
        MapSceneUIToManagers(scene);
        WireSceneInteractables(scene);
    }

    // Nếu có nhiều EventSystem (ví dụ một trong Persistent Managers và một trong Scene),
    // giữ lại EventSystem ưu tiên (thuộc PersistentGameManager nếu có),
    // và disable các EventSystem khác để tránh exception của Unity.
    private void EnsureSingleEventSystem()
    {
        EventSystem[] systems = FindObjectsOfType<EventSystem>(true);
        if (systems == null || systems.Length <= 1) return;

        EventSystem preferred = null;
        // ưu tiên EventSystem con của PersistentGameManager
        foreach (var s in systems)
        {
            if (s.gameObject.transform.IsChildOf(this.transform))
            {
                preferred = s;
                break;
            }
        }
        if (preferred == null)
        {
            // nếu không có, chọn EventSystem hiện đang active
            foreach (var s in systems) if (s.gameObject.activeInHierarchy) { preferred = s; break; }
            if (preferred == null) preferred = systems[0];
        }

        foreach (var s in systems)
        {
            if (s == preferred) continue;
            if (s.gameObject.activeInHierarchy)
            {
                s.gameObject.SetActive(false);
                Debug.Log($"PersistentGameManager: disabled duplicate EventSystem '{s.gameObject.name}' from scene.");
            }
        }
    }
}