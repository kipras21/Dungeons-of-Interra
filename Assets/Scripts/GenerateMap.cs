
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public class GenerateMap : NetworkBehaviour
{
    int[,] map;
    TileMapGenerator TMScr;
    NetworkList<int> netMap;
    List<int[]> mobMap;
    List<int[]> torchMap;
    List<int[]> boxMap;
    List<int[]> chestMap;
    List<GameObject> mobList;
    List<GameObject> mobList2;
    List<GameObject> boxList;
    List<GameObject> chestList;
    public int rows;
    public int cols;
    public List<Tile> tiles;
    public Tilemap tMapFloor;
    public Tilemap tMapWall;
    public Tilemap tMapHole;
    public Tilemap tMapDecorWall;
    public GameObject mob;
    public GameObject mob2;
    public GameObject box;
    public GameObject chest;
    public GameObject TorchLight;
    public GameObject exitTile;
    private List<int> wallTiles = new List<int>{1, 2, 3, 4, 5, 6, 8, 9, 11, 12, 14, 15, 17, 18, 21, 22, 24, 25};
    private List<int> wDecorTiles = new List<int>{10, 13, 16, 19, 20, 23};
    private List<int> floorTiles = new List<int>{0, 29, 30, 31, 32, 33, 34};
    private int[] selectedTile = new int[] { 0, 0 };

    private int[] exitLocation;

    public GameObject loadingUI;



    [SerializeField]
    private bool selfShadows = true;

    public CompositeCollider2D shadowMap;
    static readonly FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D).Assembly.GetType("UnityEngine.Rendering.Universal.ShadowUtility").GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

    private void Awake()
    {
        netMap = new NetworkList<int>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
        StartCoroutine(GenLogic());
    }

    IEnumerator GenLogic()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
            tempPlScr.SetInputClientRPC(false);
        }
        yield return new WaitForSeconds(0.1f);
        if (IsHost)
        {
            GameObject.FindGameObjectWithTag("Manager").GetComponent<WorldManager>().maxPlayers = NetworkManager.ConnectedClients.Count();
            TMScr = new TileMapGenerator(rows, cols);
            map = TMScr.GenerateMapArray();
            int[] temp;
            torchMap = genLocations(map, 7, temp = new int[] { 0, 0 }, 50, 21);
            AddTorches(torchMap);
            yield return new WaitForSeconds(0.1f);
            MapToList(map);
            //yield return new WaitForSeconds(0.1f);
            ////map = ListToMap(rows, cols);
            //yield return new WaitForSeconds(0.1f);
            while (true)
            {
                selectedTile[0] = Random.Range(0, 49);
                selectedTile[1] = Random.Range(0, 49);
                if (map[selectedTile[0], selectedTile[1]] == 0 && map[selectedTile[0] + 1, selectedTile[1]] == 0)
                    break;
            }
            mobMap = genLocations(map, 4, selectedTile, Random.Range(15, 30), 0);
            boxMap = genLocations(map, 5, selectedTile, Random.Range(15, 40), 0);                     
            chestMap = genLocations(map, 15, selectedTile, Random.Range(0, 3), 0);                     
            DespawnObjects(mobList);
            mobList = new List<GameObject>();
            yield return new WaitForSeconds(0.1f);
            foreach (int[] coord in mobMap)
            {
                GameObject sMob = Instantiate(mob, new Vector3(coord[0], coord[1] + 0.5f, 0), Quaternion.identity);
                sMob.GetComponent<NetworkObject>().Spawn(true);
                mobList.Add(sMob);
            }
            mobMap = genLocations(map, 5, selectedTile, Random.Range(5, 10), 0);
            DespawnObjects(mobList2);
            mobList2 = new List<GameObject>();
            yield return new WaitForSeconds(0.1f);
            foreach (int[] coord in mobMap)
            {
                GameObject sMob = Instantiate(mob2, new Vector3(coord[0], coord[1] + 0.5f, 0), Quaternion.identity);
                sMob.GetComponent<NetworkObject>().Spawn(true);
                mobList2.Add(sMob);
            }
            DespawnObjects(boxList);
            boxList = new List<GameObject>();
            yield return new WaitForSeconds(0.1f);
            foreach (int[] coord in boxMap)
            {
                GameObject sBox = Instantiate(box, new Vector3(coord[0], coord[1], 0), Quaternion.identity);
                sBox.GetComponent<NetworkObject>().Spawn(true);
                boxList.Add(sBox);
            }
            DespawnObjects(chestList);
            chestList = new List<GameObject>();
            yield return new WaitForSeconds(0.1f);
            foreach (int[] coord in chestMap)
            {
                GameObject sChest = Instantiate(chest, new Vector3(coord[0], coord[1], 0), Quaternion.identity);
                sChest.GetComponent<NetworkObject>().Spawn(true);
                chestList.Add(sChest);
            }
            int kk = 0;
            foreach(GameObject player in players)
            {               
                player.transform.position = new Vector2(selectedTile[0] + kk, selectedTile[1]+0.7f);
                PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                tempPlScr.ActivateUI();
                tempPlScr.mainUI.UpdateIcons(tempPlScr.netPotUses.Value);
                tempPlScr.clientUI.UpdateIcons(tempPlScr.netPotUses.Value);
                kk++;
            }
            yield return new WaitForSeconds(0.3f);
            foreach (GameObject player in players)
            {
                PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                tempPlScr.SetInputClientRPC(true);
            }
        }


        if (!IsHost)
        {
            yield return new WaitForSeconds(0.9f);
            map = ListToMap(rows, cols);
            
            foreach (GameObject player in players)
            {
                PlayerMovement tempPlScr = player.GetComponent<PlayerMovement>();
                tempPlScr.ActivateUI();
                tempPlScr.mainUI.UpdateIcons(tempPlScr.netPotUses.Value);
                tempPlScr.clientUI.UpdateIcons(tempPlScr.netPotUses.Value);
            }           
        }

        exitLocation = genLocations(map, 15, selectedTile, 1, 0)[0];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols + 5; j++)
            {
                SetTile(map[i, j], i, j);
            }
        }
        yield return new WaitForSeconds(0.1f);
        if(IsHost)
        Instantiate(exitTile, new Vector2(exitLocation[0], exitLocation[1]), Quaternion.identity).GetComponent<NetworkObject>().Spawn(true);
        GenerateShadows();
        loadingUI.SetActive(false);
    }
    private void SetTile(int tile, int x, int y)
    {
        if (tile == 0)
        {
            if (Random.Range(0, 10) == 5)
                tMapFloor.SetTile(new Vector3Int(x, y, 0), tiles[floorTiles[Random.Range(0, floorTiles.Count)]]);
            else tMapFloor.SetTile(new Vector3Int(x, y, 0), tiles[tile]);
        }
        else if (wallTiles.Contains(tile))
        {
            tMapWall.SetTile(new Vector3Int(x, y, 0), tiles[tile]);
        }
        else if (tile == 7)
        {
            if(map[x,y+1] != 7)
            {
                tMapHole.SetTile(new Vector3Int(x, y, 0), tiles[27]);
            }
            else tMapHole.SetTile(new Vector3Int(x, y, 0), tiles[tile]);
        }
        else if (wDecorTiles.Contains(tile))
        {
            tMapDecorWall.SetTile(new Vector3Int(x, y, 0), tiles[tile]);
        }
        else
        {
            tMapWall.SetTile(new Vector3Int(x, y, 0), tiles[26]);
            Instantiate(TorchLight, new Vector3(x, y, 0), Quaternion.identity).GetComponentInChildren<Animator>().speed = Random.Range(0.7f, 1.4f);
        }
    }

    public void MapToList(int[,] _map)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols + 5; j++)
            {
                netMap.Add(_map[i, j]);
            }
        }
        netMap.SetDirty(true);
    }

    public int[,] ListToMap(int _rows, int _cols)
    {
        int[,] _map = new int[_rows, _cols + 5];

        for (int i = 0; i < _rows; i++)
        {
            for (int j = 0; j < _cols + 5; j++)
            {
                _map[i, j] = netMap[i * (_cols + 5) + j];
            }
        }

        return _map;
    }

    public void AddTorches(List<int[]> torches)
    {
        foreach(int[] torch in torches)
        {
            map[torch[0], torch[1]] = 26;
        }
    }

    public List<int[]> genLocations(int[,] tileIDs, int scatterDistance, int[] playerSpawn, int amount, int validTile)
    {
        List<int[]> Locations = new List<int[]>();
        int numRows = tileIDs.GetLength(0);
        int numCols = tileIDs.GetLength(1);
        int kk = 0;

        while (amount > 0  && kk < 1500)
        {
            kk++;
            int randomRow = Random.Range(0, numRows);
            int randomCol = Random.Range(0, numCols);

            if (tileIDs[randomRow, randomCol] == validTile && DistanceToNearestLocations(randomRow, randomCol, Locations) >= scatterDistance && !DistanceToCoord(randomRow, randomCol, playerSpawn, 10))
            {
                Locations.Add(new int[] { randomRow, randomCol });
                amount--;
            }
        }

        return Locations;
    }

        int DistanceToNearestLocations(int row, int col, List<int[]> locations)
        {
            int minDistance = int.MaxValue;

            foreach (int[] location in locations)
            {
                int distance = Math.Abs(location[0] - row) + Math.Abs(location[1] - col);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        bool DistanceToCoord(int row, int col, int[] coord, int minDistance)
        {
            int distance = Math.Abs(coord[0] - row) + Math.Abs(coord[1] - col);
            if (distance < minDistance)
            {
                return true;
            }
            else return false;
        }



    public void GenerateShadows()
    {
        DestroyOldShadowCasters();

        Vector2[] pathVertices1 = new Vector2[shadowMap.GetPathPointCount(0)];
        Vector2[] pathVertices2 = new Vector2[shadowMap.GetPathPointCount(1)];
        shadowMap.GetPath(0, pathVertices1);
        shadowMap.GetPath(1, pathVertices2);
        Vector2[] tempVertices = new Vector2[1];
        tempVertices[0] = pathVertices2[0] - new Vector2(0, 1);
        pathVertices1 = pathVertices1.Concat(tempVertices).ToArray();
        Vector2[] pathVerticeMain = pathVertices1.Concat(pathVertices2).Append(pathVertices2[0] - new Vector2(0, 1)).ToArray();
        
        GameObject shadowCasterMain = new GameObject("Shadow" + 0);
        shadowCasterMain.transform.parent = gameObject.transform;
        ShadowCaster2D shadowCasterMainComponent = shadowCasterMain.AddComponent<ShadowCaster2D>();
        shadowCasterMainComponent.selfShadows = this.selfShadows;
        var fieldInfo = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.Instance | BindingFlags.NonPublic);
        fieldInfo.SetValue(shadowCasterMainComponent, new[] { 0, -742061297, 2132180465});

        Vector3[] testPathMain = new Vector3[pathVerticeMain.Length];
        for (int j = 0; j < pathVerticeMain.Length; j++)
        {
            testPathMain[j] = pathVerticeMain[j];
        }

        shapePathField.SetValue(shadowCasterMainComponent, testPathMain);
        shapePathHashField.SetValue(shadowCasterMainComponent, Random.Range(int.MinValue, int.MaxValue));
        meshField.SetValue(shadowCasterMainComponent, new Mesh());
        generateShadowMeshMethod.Invoke(shadowCasterMainComponent,
        new object[] { meshField.GetValue(shadowCasterMainComponent), shapePathField.GetValue(shadowCasterMainComponent) });


        for (int i = 2; i < shadowMap.pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[shadowMap.GetPathPointCount(i)];
            shadowMap.GetPath(i, pathVertices);
            GameObject shadowCaster = new GameObject("Shadow" + i);
            shadowCaster.transform.parent = gameObject.transform;
            ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
            shadowCasterComponent.selfShadows = this.selfShadows;
            fieldInfo.SetValue(shadowCasterComponent, new[] { 0, -742061297, 2132180465 });

            Vector3[] testPath = new Vector3[pathVertices.Length];
            for (int j = 0; j < pathVertices.Length; j++)
            {
                testPath[j] = pathVertices[j];
            }

            shapePathField.SetValue(shadowCasterComponent, testPath);
            shapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
            meshField.SetValue(shadowCasterComponent, new Mesh());
            generateShadowMeshMethod.Invoke(shadowCasterComponent,
            new object[] { meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent) });
        }
        
    }
    public void DestroyOldShadowCasters()
    {

        var tempList = shadowMap.transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
    }


    public void DespawnObjects(List<GameObject> listToDespawn)
    {
        if(listToDespawn != null)
        foreach (GameObject obj in listToDespawn)
        {
            obj.GetComponent<NetworkObject>().Despawn();
            Destroy(obj);
        }
    }
    public override void OnDestroy()
    {
        try
        {
            netMap.Dispose();
            DespawnObjects(mobList);
            DespawnObjects(boxList);
        }
        catch
        {

        }
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netMap.OnListChanged += OnListChanged;
    }

    void OnListChanged(NetworkListEvent<int> changeEvent)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}




public class TileMapGenerator
{
    private const int EMPTY_TILE = 0;
    private const int WALL_TILE = 1;
    private const int EXIT_TILE = 2;
    private const int PLAYER_SPAWN_TILE = 3;

    public int MAP_WIDTH;
    public int MAP_HEIGHT;

    private const int MIN_ROOM_SIZE = 3;
    private const int MAX_ROOM_SIZE = 6;

    private const float ROOM_DENSITY = 0.05f;

    private List<int> cornerCheck = new List<int> { 0, 7 };
    private List<int> lEdgeCheck = new List<int> { 1, 8, 9, 21};
    private List<int> rEdgeCheck = new List<int> { 1, 14, 15, 21 };
    private List<int> topEdgeLeftCheck = new List<int> {15, 21, 22, 23};
    private List<int> topEdgeRightCheck = new List<int> {9, 10, 11, 21, 22, 23};

    private int[,] map;

    public TileMapGenerator(int rows, int cols)
    {
        MAP_HEIGHT = rows;
        MAP_WIDTH = cols;
        return;
    }

    public int CountTiles(int tileID, int[,] _map)
    {
        int _count = 0;
        for (int i = 0; i < _map.GetLength(0); i++)
        {
            for (int j = 0; j < _map.GetLength(1); j++)
            {
                if (_map[i, j] == tileID)
                {
                    _count++;
                }
            }
        }
        return _count;
    }

    public int[,] GenerateMapArray()
    {
        int whileCount = 0;
        map = new int[MAP_WIDTH, MAP_HEIGHT + 5];
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT + 5; y++)
            {
                map[x, y] = WALL_TILE;
            }
        }
        while (CountTiles(0, map) < MAP_WIDTH * MAP_HEIGHT * 0.6f && whileCount < 50)
        {
            whileCount++;

            
            for (int x = 0; x < MAP_WIDTH; x++)
            {
                for (int y = 0; y < MAP_HEIGHT+5; y++)
                {
                    map[x, y] = WALL_TILE;
                }
            }

            
            int numRooms = (int)(MAP_WIDTH * MAP_HEIGHT * ROOM_DENSITY);
            Room[] rooms = new Room[numRooms];
            for (int i = 0; i < numRooms; i++)
            {
                
                int width = Random.Range(MIN_ROOM_SIZE, MAX_ROOM_SIZE + 1);
                int height = Random.Range(MIN_ROOM_SIZE, MAX_ROOM_SIZE + 1);

                
                int x = Random.Range(1, MAP_WIDTH - width - 1);
                int y = Random.Range(1, MAP_HEIGHT - height - 1);

                
                Room room = new Room(x, y, width, height);
                rooms[i] = room;

                
                for (int rx = x; rx < x + width; rx++)
                {
                    for (int ry = y; ry < y + height; ry++)
                    {
                        map[rx, ry] = EMPTY_TILE;
                    }
                }
            }

            for (int i = 0; i < numRooms; i++)
            {
                Room roomA = rooms[i];
                Room roomB = FindClosestRoom(roomA, rooms);

                int startX = roomA.centerX;
                int startY = roomA.centerY;
                int endX = roomB.centerX;
                int endY = roomB.centerY;

                while (startX != endX)
                {
                    map[startX, startY] = EMPTY_TILE;
                    startX += (startX < endX) ? 1 : -1;
                }

                while (startY != endY)
                {
                    map[startX, startY] = EMPTY_TILE;
                    startY += (startY < endY) ? 1 : -1;
                }
            }



            ConnectRooms(MAP_WIDTH, MAP_HEIGHT, map);
        }
        SeedHoles(map);
        SeedWalls(map);
        GrowWalls(map);
        SeedRooves(map);
        SeedRoofEdges(map);
        GrowRoofEdges(map);
        FinalizeEdges(map);
        return map;
    }

    private void SeedHoles(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                switch (x)
                {
                    case 0:                     
                        break;
                    case var value when value == MAP_WIDTH - 1:                   
                        break;
                    default:
                        if (map[x, y] == 1)
                        {
                            if (y > 0)
                            {
                                if (cornerCheck.Contains(map[x, y + 1]) && cornerCheck.Contains(map[x, y - 1]))
                                {
                                    map[x, y] = 7;
                                }
                            }
                            if (cornerCheck.Contains(map[x + 1, y]) && cornerCheck.Contains(map[x - 1, y]))
                            {
                                map[x, y] = 7;
                            }                           
                        }
                        break;
                }
            }
        }
    }

    private void SeedWalls(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                switch (x)
                {
                    case 0:
                        if (y == 0)
                        {
                            map[x, y] = 2;
                        }
                        else map[x, y] = 3;
                        break;
                    case var value when value == MAP_WIDTH - 1:
                        if (y != 0)
                        {
                            map[x, y] = 6;
                        }
                        break;
                    default:
                        if (map[x, y] == 1)
                        {
                            if (cornerCheck.Contains(map[x + 1, y]) && cornerCheck.Contains(map[x, y - 1]))
                            {
                                if (map[x, y + 1] == 1)
                                    map[x, y] = 8;
                                else map[x, y] = 7;
                            }
                            else if (cornerCheck.Contains(map[x - 1, y]) && cornerCheck.Contains(map[x, y - 1]))
                            {
                                if (map[x, y + 1] == 1)
                                    map[x, y] = 14;
                                else map[x, y] = 7;
                            }
                           
                        }
                        break;
                }

                switch (y)
                {
                    case 0:
                        if(x != 0 && x < MAP_WIDTH - 1)
                        {
                            map[x, y] = 4;
                        }
                        else if (x == MAP_WIDTH - 1)
                        {
                            map[x, y] = 5;
                        }
                        break;
                }       
            }
        }
    }

    private void GrowWalls(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                switch (x)
                {
                    case 0:                       
                        break;
                    case var value when value == MAP_WIDTH - 1:                     
                        break;
                    default:
                        switch(map[x,y])
                        {
                            case 8:
                                if (map[x, y + 1] == 1)
                                {
                                    map[x, y + 1] = 9;
                                    if (cornerCheck.Contains(map[x + 1, y + 2]) && !cornerCheck.Contains(map[x + 1, y]))
                                    {
                                        map[x, y] = 7;
                                        map[x, y + 1] = 7;
                                        if (!cornerCheck.Contains(map[x, y + 2]))
                                        {
                                            map[x, y + 2] = 7;
                                        }                                        
                                    }
                                    else
                                    {
                                        if (map[x, y + 2] != 0)
                                        {
                                            map[x, y + 2] = 11;
                                        }
                                        else map[x, y + 2] = 10;
                                    }
                                }
                                else map[x, y] = 7;
                                break;

                            case 14:
                                if (map[x, y + 1] == 1)
                                {
                                    map[x, y + 1] = 15;
                                    if (cornerCheck.Contains(map[x - 1, y + 2]) && !cornerCheck.Contains(map[x - 1, y]))
                                    {
                                        map[x, y] = 7;
                                        map[x, y + 1] = 7;
                                        if (!cornerCheck.Contains(map[x, y + 2]))
                                        {
                                            map[x, y + 2] = 7;
                                        }
                                    }
                                    else
                                    {                                    
                                        if (map[x, y + 2] != 0)
                                        {
                                            map[x, y + 2] = 17;
                                        }
                                        else map[x, y + 2] = 16;
                                    }
                                }
                                else map[x, y] = 7;
                                break;
                        }
                        if (map[x, y] == 1)
                        {
                            if (map[x + 1, y] == 0 && map[x - 1, y] == 0)
                            {
                                map[x, y] = 7;
                            }
                            else if (map[x + 1, y] != 0 && map[x - 1, y] != 0 && map[x, y+1] != 0 && cornerCheck.Contains(map[x, y - 1]))
                            {
                                map[x, y + 1] = 21;
                                if (map[x, y + 2] == 1 || map[x, y + 2] == 7)
                                {
                                    map[x, y + 2] = 22;
                                }
                                else map[x, y + 2] = 23;

                            }
                            else if (y > 0)
                            {
                                if (map[x, y + 1] == 0 && map[x, y - 1] == 0)
                                {
                                    map[x, y] = 7;
                                }
                            }
                        }
                        break;
                }

            }
        }
    }

    private void SeedRooves(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                switch (x)
                {
                    case 0:                       
                        break;
                    case var value when value == MAP_WIDTH - 1:                       
                        break;
                    default:
                        switch (map[x, y])
                        {
                            case 1:
                                if (cornerCheck.Contains(map[x, y + 1]))
                                {
                                    //map[x, y] = 13;
                                }
                                break;
                            case 16:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y+2]))
                                    map[x, y + 1] = 20;
                                else if (cornerCheck.Contains(map[x, y + 2])) 
                                    map[x, y + 1] = 12;
                                break;
                            case 17:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 20;
                                else if(cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 12;
                                break;
                            case 10:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 19;
                                else if (cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 18;
                                break;
                            case 11:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 19;
                                else if (cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 18;
                                break;
                            case 22:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 13;
                                else if (cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 4;
                                break;
                            case 23:
                                if (map[x, y + 1] == 0 && cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 13;
                                else if (cornerCheck.Contains(map[x, y + 2]))
                                    map[x, y + 1] = 4;
                                break;
                        }
                        break;
                }             
            }
        }
    }

    private void SeedRoofEdges(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 1; y < MAP_HEIGHT; y++)
            {
                switch (x)
                {
                    case 0:
                        break;
                    case var value when value == MAP_WIDTH - 1:
                        break;
                    default:
                        switch (map[x, y])
                        {
                            case 1:
                                if (cornerCheck.Contains(map[x + 1, y]) && !cornerCheck.Contains(map[x, y + 1]))
                                {
                                    if (!cornerCheck.Contains(map[x - 1, y]))
                                        map[x, y] = 3;
                                    else map[x, y] = 7;
                                }
                                else if ((map[x, y - 1] == 3 || map[x, y - 1] == 11) && lEdgeCheck.Contains(map[x + 1, y]) && !cornerCheck.Contains(map[x, y + 1]))
                                {
                                    if(map[x-1,y] !=4 || map[x - 1, y] != 13)
                                    map[x, y] = 3;
                                    else map[x, y] = 18;
                                }
                                else if (cornerCheck.Contains(map[x - 1, y]) && !cornerCheck.Contains(map[x, y + 1]))
                                {
                                    if(!cornerCheck.Contains(map[x + 1, y]))
                                    map[x, y] = 6;
                                    else map[x, y] = 7;
                                }
                                else if ((map[x, y - 1] == 6 || map[x, y - 1] == 17) && rEdgeCheck.Contains(map[x - 1, y]) && !cornerCheck.Contains(map[x, y + 1]))
                                {
                                    map[x, y] = 6;
                                }
                                else if (cornerCheck.Contains(map[x, y + 1]) && !cornerCheck.Contains(map[x + 1, y]) && !cornerCheck.Contains(map[x - 1, y]))
                                {
                                    if (!topEdgeLeftCheck.Contains(map[x - 1, y]) && !topEdgeRightCheck.Contains(map[x + 1, y]) || (map[x + 1, y] == 1 && !cornerCheck.Contains(map[x + 1, y - 1])) || map[x + 1, y] == 18 || map[x + 1, y] == 19)
                                    {
                                        map[x, y] = 4;
                                    }
                                    else if (map[x - 1, y] == 16 || map[x - 1, y] == 17)
                                    {
                                        map[x, y] = 2;
                                    }
                                    else if (topEdgeLeftCheck.Contains(map[x - 1, y]))
                                    {
                                        map[x, y] = 12;
                                    }
                                    else map[x, y] = 18;
                                }
                                else if (cornerCheck.Contains(map[x, y + 1]) && cornerCheck.Contains(map[x + 1, y]))
                                {
                                    map[x, y] = 18;
                                }
                                else if (cornerCheck.Contains(map[x, y + 1]) && cornerCheck.Contains(map[x - 1, y]))
                                {
                                    map[x, y] = 12;
                                }
                                break;
                        }
                        break;
                }
            }
        }
    }

    private void GrowRoofEdges(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 1; y < MAP_HEIGHT+5; y++)
            {
                switch (x)
                {
                    case 0:
                        break;
                    case var value when value == MAP_WIDTH - 1:
                        break;
                    default:
                        switch (map[x, y])
                        {
                            case 12:
                                if (map[x + 1, y] == 0)
                                {
                                    map[x + 1, y] = 19;
                                }
                                if (map[x + 1, y - 1] == 0)
                                {
                                    map[x + 1, y - 1] = 10;
                                }
                                break;
                            case 20:
                                if (map[x + 1, y] == 0)
                                {
                                    map[x + 1, y] = 19;
                                }
                                if (map[x + 1, y - 1] == 0)
                                {
                                    map[x + 1, y - 1] = 10;
                                }
                                break;
                            case 18:
                                if (map[x - 1, y] == 0) 
                                {
                                    map[x - 1, y] = 20;
                                }
                                if (map[x - 1, y - 1] == 0) 
                                {
                                    map[x - 1, y - 1] = 16;
                                }
                                break;
                            case 19:
                                if (map[x - 1, y] == 0)
                                {
                                    map[x - 1, y] = 20;
                                }
                                if (map[x - 1, y - 1] == 0)
                                {
                                    map[x - 1, y - 1] = 16;
                                }
                                break;
                            case 13:
                                if(y > 2)
                                {
                                    if(map[x-1,y] == 0 && map[x, y-2] == 21)
                                    {
                                        map[x, y] = 20;
                                        if (map[x, y - 1] == 23)
                                            map[x, y - 1] = 16;
                                        else map[x, y - 1] = 17;
                                        
                                        map[x, y - 2] = 15;
                                        if (map[x, y - 3] == 1)
                                        map[x, y - 3] = 14;
                                    }
                                    else if(map[x + 1, y] == 0 && map[x, y - 2] == 21)
                                    {
                                        map[x, y] = 19;
                                        if (map[x, y - 1] == 23)
                                            map[x, y - 1] = 10;
                                        else map[x, y - 1] = 11;
                                        map[x, y - 2] = 9;
                                        if (map[x, y - 3] == 1)
                                            map[x, y - 3] = 8;
                                    }
                                }
                                break;
                            case 4:
                                if (y > 2)
                                {
                                    if (map[x - 1, y] == 0 && map[x, y - 2] == 21)
                                    {
                                        map[x, y] = 12;
                                        if (map[x, y - 1] == 23)
                                            map[x, y - 1] = 16;
                                        else map[x, y - 1] = 17;
                                        map[x, y - 2] = 15;
                                        map[x, y - 3] = 14;
                                    }
                                    else if (map[x + 1, y] == 0 && map[x, y - 2] == 21)
                                    {
                                        map[x, y] = 18;
                                        if (map[x, y - 1] == 23)
                                            map[x, y - 1] = 10;
                                        else map[x, y - 1] = 11;
                                        map[x, y - 2] = 9;
                                        map[x, y - 3] = 8;
                                    }
                                }
                                break;
                            case 1:
                                if (!cornerCheck.Contains(map[x, y-1]))
                                {
                                    map[x, y] = 24;
                                }
                                break;

                        }
                        break;
                }
            }
        }
    }

    private void FinalizeEdges(int[,] map)
    {
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT+5; y++)
            {
                if(y>0)
                switch (x)
                {
                    case 0:
                        if(map[x+1,y] == 24 || map[x + 1, y] == 3)
                        {
                            map[x, y] = 24;
                        }
                        else if (map[x + 1, y] == 10 || map[x + 1, y] == 11 || map[x + 1, y] == 22)
                        {
                            map[x, y] = 25;
                        }
                        else if (map[x + 1, y] == 18 || map[x + 1, y] == 19 || map[x + 1, y] == 4)
                        {
                            map[x, y] = 2;
                        }
                        break;
                    case var value when value == MAP_WIDTH - 1:
                            if (map[x - 1, y] == 24 || map[x - 1, y] == 6)
                            {
                                map[x, y] = 24;
                            }
                            else if (map[x - 1, y] == 18 || map[x - 1, y] == 19 || map[x - 1, y] == 4)
                            {
                                map[x, y] = 5;
                            }
                            break;
                }

                switch (y)
                {
                    case 0:
                       if(x == 0 && !cornerCheck.Contains(map[x+1, y+1]))
                        {
                            map[x, y] = 24;
                        }
                       else if(x!= 0 && x!= MAP_WIDTH-1)
                        {
                            if(map[x,y+1] == 24 || map[x, y + 1] == 13 || map[x, y + 1] == 4)
                            {
                                map[x, y] = 24;
                            }
                            else if (map[x,y+1] == 3 || map[x, y + 1] == 18 || map[x, y + 1] == 19)
                            {
                                map[x, y] = 2;
                            }
                            else if(map[x,y+1] == 6 || map[x, y + 1] == 20 || map[x, y + 1] == 12)
                            {
                                map[x, y] = 5;
                            }
                        }
                       else if(x == MAP_WIDTH-1)
                        {
                            if(!cornerCheck.Contains(map[x-1, y+1]) || map[x - 1, y + 1] == 24)
                            {
                                map[x, y] = 24;
                            }
                        }
                        break;
                }
            }
        }
    }

    private Room FindClosestRoom(Room room, Room[] rooms)
    {
        int closestDistance = int.MaxValue;
        Room closestRoom = null;

        foreach (Room otherRoom in rooms)
        {
            if (otherRoom == room) continue;

            int distance = room.DistanceTo(otherRoom);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRoom = otherRoom;
            }
        }

        return closestRoom;
    }

    private class Room
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public int centerX
        {
            get { return x + (width / 2); }
        }

        public int centerY
        {
            get { return y + (height / 2); }
        }

        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public int DistanceTo(Room other)
        {
            int dx = centerX - other.centerX;
            int dy = centerY - other.centerY;
            return dx * dx + dy * dy;
        }
    }

    public void ConnectRooms(int _width, int _height, int[,] _map)
    {
        
        List<System.Tuple<int, int>> walkableSpaces = new List<System.Tuple<int, int>>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_map[x, y] == 0)
                {
                    walkableSpaces.Add(new System.Tuple<int, int>(x, y));
                }
            }
        }

        
        HashSet<System.Tuple<int, int>> visited = new HashSet<System.Tuple<int, int>>();
        Queue<System.Tuple<int, int>> queue = new Queue<System.Tuple<int, int>>();

        System.Tuple<int, int> start = walkableSpaces[0];
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            System.Tuple<int, int> current = queue.Dequeue();
            int x = current.Item1;
            int y = current.Item2;

            
            List<System.Tuple<int, int>> adjacent = new List<System.Tuple<int, int>>();
            if (x > 0 && _map[x - 1, y] == 0) adjacent.Add(new System.Tuple<int, int>(x - 1, y));
            if (x < _width - 1 && _map[x + 1, y] == 0) adjacent.Add(new System.Tuple<int, int>(x + 1, y));
            if (y > 0 && _map[x, y - 1] == 0) adjacent.Add(new System.Tuple<int, int>(x, y - 1));
            if (y < _height - 1 && _map[x, y + 1] == 0) adjacent.Add(new System.Tuple<int, int>(x, y + 1));

            
            foreach (System.Tuple<int, int> next in adjacent)
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

       
        foreach (System.Tuple<int, int> space in walkableSpaces)
        {
            if (!visited.Contains(space))
            {
                _map[space.Item1, space.Item2] = 1;
            }
        }
    }
}
