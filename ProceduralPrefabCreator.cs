using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ProceduralPrefabCreator : EditorWindow
{
    // Tool Variables
    int randomQuantity          = 1;
    int randomIndex             = 0;
    int prefabCounter           = 0;

    public List<Object> listOne = new List<Object>(1);
    public List<Object> listTwo = new List<Object>(1);

    GameObject          currentObject;

    Vector3             baseExtents, decExtents;
    int                 listOneSize, oldListOneSize, tempListOneSize;
    int                 listTwoSize, oldListTwoSize, tempListTwoSize;
    int                 minQuantity, maxQuantity;
    bool                showListOne, showListTwo, showCreateOptions, showFinalOptions;
    string              tempString, prefabName, currentName, prefabFolder;
    float               minRotation, maxRotation;
    float               minScale, maxScale;
    float               tolerance;

    // Show in context menu
    [MenuItem("ShadeForm/Procedural Prefab Creator")]
    static void ProceduralSliceCreator()
    {
        // Create instance of window if one doesn't already exist
        EditorWindow.GetWindow(typeof(ProceduralPrefabCreator));
    }

    // When window opens, load all previous values
    void OnEnable ()
    {       
        showListOne     = EditorPrefs.GetBool( "Prcdrl_Show_List_One", false );
        showListTwo     = EditorPrefs.GetBool( "Prcdrl_Show_List_Two", false );
        
        listOneSize     = EditorPrefs.GetInt( "Prcdrl_List_Size_One", 0 );
        oldListOneSize  = listOneSize;
        tempListOneSize = listOneSize;
        
        // (Re)Populate the base prefab list to the correct size
        for ( int i = 0; i < listOneSize; i++ )
            listOne.Add( new Object() );

        // Load previous prefabs into the base prefabs list
        // NOTE: This uses the prefab asset's path in the "Resources" folder
        //       so changing the name/path of the prefab asset will cause it not to load
        for ( int i = 0; i < listOne.Count; i++ )
        {
            tempString = EditorPrefs.GetString("Prcdrl_Base_Object_" + i, "NULL");

            if (tempString != "NULL")
                listOne[i] = Resources.Load(tempString);
        }

        listTwoSize     = EditorPrefs.GetInt("Prcdrl_List_Size_Two", 0);
        oldListTwoSize  = listTwoSize;
        tempListTwoSize = listTwoSize;

        // (Re)Populate the decorator prefab list to the correct size
        for (int i = 0; i < listTwoSize; i++)
            listTwo.Add(new Object());

        // Load previous prefabs into the decorators prefabs list
        // NOTE: This uses the prefab asset's path in the "Resources" folder
        //       so changing the name/path of the prefab asset will cause it not to load
        for (int i = 0; i < listTwo.Count; i++)
        {
            tempString = EditorPrefs.GetString("Prcdrl_Dec_Object_" + i, "NULL");

            if (tempString != "NULL")
                listTwo[i] = Resources.Load(tempString);
        }

        minQuantity   = EditorPrefs.GetInt( "Prcdrl_Min_Quantity", 1 );
        maxQuantity   = EditorPrefs.GetInt( "Prcdrl_Max_Quantity", 5 );

        minRotation   = EditorPrefs.GetFloat( "Prcdrl_Min_Rotation", 0f );
        maxRotation   = EditorPrefs.GetFloat( "Prcdrl_Max_Rotation", 180f );

        minScale      = EditorPrefs.GetFloat( "Prcdrl_Min_Scale", 1f );
        maxScale      = EditorPrefs.GetFloat( "Prcdrl_Max_Scale", 1.2f );

        tolerance     = EditorPrefs.GetFloat( "Prcdrl_Tolerence", 1f );

        prefabName    = EditorPrefs.GetString( "Prcdrl_Name", "Procedural_Prefab_" );
        currentName   = EditorPrefs.GetString( "Prcdrl_Current_Name", null );
        prefabFolder  = EditorPrefs.GetString( "Prcdrl_Folder", "Prefabs" );
        prefabCounter = EditorPrefs.GetInt( "Prcdrl_Counter", 1 );
    }

    // When window is closed, save all current values
    void OnDisable ()
    {
        EditorPrefs.SetBool( "Prcdrl_Show_List_One", showListOne );
        EditorPrefs.SetBool( "Prcdrl_Show_List_Two", showListTwo );
        EditorPrefs.SetInt( "Prcdrl_List_Size_One", listOneSize );
        EditorPrefs.SetInt( "Prcdrl_List_Size_Two", listTwoSize );

        // Save prefab asset's filename. This will be used as its path in the "Resources" folder
        // Prefabs must be stored at the root level of a folder named "Resources" in order for them to load later
        for (int i = 0; i < listOne.Count; i++)
        {
            if ( listOne[i] != null )
                EditorPrefs.SetString("Prcdrl_Base_Object_" + i, listOne[i].name);
            else
                EditorPrefs.SetString("Prcdrl_Base_Object_" + i, "NULL" );
        }

        for (int i = 0; i < listTwo.Count; i++)
        {
            if (listTwo[i] != null)
                EditorPrefs.SetString("Prcdrl_Dec_Object_" + i, listTwo[i].name);
            else
                EditorPrefs.SetString("Prcdrl_Dec_Object_" + i, "NULL");
        }

        EditorPrefs.SetInt( "Prcdrl_Min_Quantity", minQuantity );
        EditorPrefs.SetInt( "Prcdrl_Max_Quantity", maxQuantity );

        EditorPrefs.SetFloat( "Prcrdl_Min_Rotation", minRotation );
        EditorPrefs.SetFloat( "Prcdrl_Max_Rotation", maxRotation );

        EditorPrefs.SetFloat( "Prcdrl_Min_Scale", minScale );
        EditorPrefs.SetFloat( "Prcdrl_Max_Scale", maxScale );

        EditorPrefs.SetFloat( "Prcdrl_Tolerence", tolerance );

        EditorPrefs.SetString( "Prcdrl_Name", prefabName );
        EditorPrefs.SetString( "Prcdrl_Current_Name", currentName );
        EditorPrefs.SetString( "Prcdrl_Folder", prefabFolder );
        EditorPrefs.SetInt( "Prcdrl_Counter", prefabCounter );
    }

    // Displays GUI elements of editor window
    public void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("-Procedural Prefab Creator-", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        //GUILayout.FlexibleSpace();

        // Foldout for Base Prefabs list
        showListOne = EditorGUILayout.Foldout( showListOne, "Base Prefabs" );

        // --Base Prefab List--
        if (showListOne)
        {
            EditorGUI.indentLevel += 2;

            // List size variable is detached from the user's input by a temporary variable that
            // is only applied to the actual list size variable when the "Update Size" button is pressed.
            // This is a (temporary?) fix/hack to avoid losing previous values when changing the size of the list
            EditorGUILayout.BeginHorizontal();
            tempListOneSize = EditorGUILayout.IntField( "Size",tempListOneSize);
            if (GUILayout.Button("Update Size"))
                listOneSize = tempListOneSize;
            EditorGUILayout.EndHorizontal();

            // Add or remove elements to the list if the size has been changed by the user
            if ( listOneSize != oldListOneSize )
            {
                if (listOneSize > oldListOneSize)
                {
                    int diff = listOneSize - oldListOneSize;

                    for (int i = 0; i < diff; i++)
                        listOne.Add(new Object());
                }
                else
                {
                    int diff = oldListOneSize - listOneSize;
                    int index = listOne.Count - 1;

                    for ( int i = 0; i < diff; i++, index-- )
                        listOne.RemoveAt(index);
                }
                oldListOneSize = listOneSize;
            }

            // Display each element of the list in sequence
            for (int i = 0; i < listOne.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                tempString = "Element " + i;
                listOne[i] = EditorGUILayout.ObjectField(tempString, listOne[i], typeof(Object), true);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel -= 2;
        }

        // Foldout for Decorators Prefabs list
        showListTwo = EditorGUILayout.Foldout(showListTwo, "Decorator Prefabs");

        // --Decorator Prefab List--
        if (showListTwo)
        {
            EditorGUI.indentLevel += 2;

            // List size variable is detached from the user's input by a temporary variable that
            // is only applied to the actual list size variable when the "Update Size" button is pressed.
            // This is a (temporary?) fix/hack to avoid losing previous values when changing the size of the list
            EditorGUILayout.BeginHorizontal();
            tempListTwoSize = EditorGUILayout.IntField("Size",tempListTwoSize);
            if (GUILayout.Button("Update Size"))
                listTwoSize = tempListTwoSize;
            EditorGUILayout.EndHorizontal();

            // Add or remove elements to the list if the size has been changed by the user
            if (listTwoSize != oldListTwoSize)
            {
                if ( listTwoSize > oldListTwoSize )
                {
                    int diff = listTwoSize - oldListTwoSize;

                    for ( int i = 0; i < diff; i++ )
                        listTwo.Add(new Object());
                }
                else
                {
                    int diff  = oldListTwoSize - listTwoSize;
                    int index = listTwo.Count - 1;

                    for ( int i = 0; i < diff; i++, index-- )
                        listTwo.RemoveAt(index);
                }
                oldListTwoSize = listTwoSize;
            }

            // Display each element of the list in sequence
            for ( int i = 0; i < listTwo.Count; i++ )
            {
                EditorGUILayout.BeginHorizontal();
                tempString = "Element" + 1;
                listTwo[i] = EditorGUILayout.ObjectField( tempString, listTwo[i], typeof(Object), true );
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel -= 2;
        }

        showCreateOptions = EditorGUILayout.Foldout( showCreateOptions, "Create Prefab Options" );

        if (showCreateOptions)
        {
            EditorGUI.indentLevel += 1;

            // Minimum and maximum range of decorators to spawn on the base. Randomly selected
            minQuantity = EditorGUILayout.IntField( "Min Decorator Quantity", minQuantity );
            maxQuantity = EditorGUILayout.IntField( "Max Decorator Quantity", maxQuantity );

            GUILayout.Space(12f);

            // Minimum and maximum range of random rotation along the Y axis for each decorator created
            minRotation = EditorGUILayout.FloatField( "Min Decorator Y Rotation", minRotation );
            maxRotation = EditorGUILayout.FloatField( "Max Decorator Y Rotation", maxRotation );

            GUILayout.Space(12f);

            // Minimum and maximum range of random rotation along the Y axis for each decorator created
            minScale    = EditorGUILayout.FloatField( "Min Decorator Scale", minScale );
            maxScale    = EditorGUILayout.FloatField( "Max Decorator Scale", maxScale );

            // When searching for a suitable location to spawn a decorator object, the tool will sample several points
            // surrounding the potential location to make sure the base mesh's topology is flat enough for the decorator
            // This variable controls the maximum amount of variance between all the checked point's height.
            tolerance   = EditorGUILayout.FloatField( "Ground Height Tolerence", tolerance );

            EditorGUI.indentLevel -= 1;
        }

        showFinalOptions = EditorGUILayout.Foldout( showFinalOptions, "Finalize Prefab Options" );

        if ( showFinalOptions )
        {
            EditorGUI.indentLevel += 1;

            prefabName    = EditorGUILayout.TextField( "Prefab Prefix", prefabName );

            // This variable increments automatically whenever a prefab is finalized
            // However, this is a way to manually change it if need be
            prefabCounter = EditorGUILayout.IntField( "Prefab Number", prefabCounter );

            GUILayout.Space(12f);

            prefabFolder  = EditorGUILayout.TextField( "Save Folder", prefabFolder );

            EditorGUI.indentLevel -= 1;
        }

        // Read-only counter that shows the current prefab number as it increases
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label( "Prefab Number: " + prefabCounter );
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // If each of the two lists have at least one element, allow user to create new procedural object
        GUI.enabled = ( listOneSize > 0 && listTwoSize > 0 && !GameObject.Find( currentName ) );
        if (GUILayout.Button("Create New Object"))
        {
            CreateObject();
        }

        GUI.enabled = ( GameObject.Find( currentName ) );
        if (GUILayout.Button("Delete Current Object and Retry"))
        {
            DestroyImmediate( GameObject.Find( prefabName + prefabCounter ) );
            CreateObject();
        }
        
        if (GUILayout.Button("Finalize Prefab and Move to Folder"))
        {
            FinalizePrefab();
        }

        GUILayout.EndVertical();
    }

    // Create the base prefab and add random decorators to it
    void CreateObject ()
    {
        // Add one to max because Random.Range function overload for integers has non-inclusive max
        randomQuantity = Random.Range(minQuantity, maxQuantity + 1);

        // Randomly pick a new slice prefab
        randomIndex = Random.Range(0, listOne.Count);

        // Create new base object
        currentObject = Instantiate( listOne[randomIndex], new Vector3(0f, 0f, 0f), Quaternion.identity ) as GameObject;

        // Pulls the extents Vector3 data from the mesh of the base object. This is half the total size of the mesh in XYZ directions
        // and is used primarily for constraints/range when randomly selecting locations for the decorator objects
        baseExtents = currentObject.GetComponent<MeshFilter>().sharedMesh.bounds.extents;

        // This will add the "randomQuantity" of decorators to the newly created base object 
        DecorateObject();

        // Set the current selection to the 
        Selection.activeGameObject = currentObject;

        // Names the object according to the preset values
        currentObject.name = prefabName + prefabCounter;
        // Caches the name of the current object in order to tell if it is still in the scene even if user changes naming convetion
        currentName        = prefabName + prefabCounter;
    }

    // Adds decorator object(s) to the base object
    void DecorateObject ()
    {
        // Apply a Physics collider that follows the exact verticies of the base object's mesh--used to check topology with raycasts
        MeshCollider  mc                = currentObject.AddComponent("MeshCollider") as MeshCollider;
        // Array of colliders used with the raycasts to make sure no objects will spawn overlapping
        BoxCollider[] bc                = new BoxCollider[randomQuantity];
        float[]       yCoordsFrmRaycast = new float[5];
        Quaternion    rotation          = Quaternion.identity;
        float         randomScale;
        Vector3       location;
        RaycastHit    hit;

        for ( int i = 0; i < randomQuantity; i++ )
        {
            // Randomly pick a decorator prefab
            randomIndex = Random.Range(0, listTwo.Count);

            // Spawn new decorator in a zeroed out location--the location will be selected/applied later--it is being spawned now so that
            // The extents data can be pulled from the mesh. The List of objects holds them as "Object" rather than "GameObject"
            // And "MeshFilter" component is not accessable from "Object" type.
            GameObject decorator = Instantiate( listTwo[randomIndex], Vector3.zero, rotation) as GameObject;
            
            // Random rotation with constraints
            rotation.eulerAngles = new Vector3( 0f, Random.Range( minRotation, maxRotation ), 0f );

            // Randomly resize decorator with constraints
            randomScale = Random.Range( minScale, maxScale );

            // Grabs the extent data 
            decorator.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            // Pulls the extents data from the mesh used when selecting the random location and checking the base object's mesh topology
            decExtents = decorator.GetComponent<MeshFilter>().sharedMesh.bounds.extents;

            // This array's elements are added to the random location for each iteration of the loop below to check if the random location
            // has suitable topology for the decorator object to be spawned there. It checks: center of loc and each of the four corners
            Vector3[] modifiers = new Vector3[5] { new Vector3 ( 0f, ( decExtents.y * 2 ) + 50f, 0f ), 
                                                   new Vector3 ( decExtents.x, ( decExtents.y * 2 ) + 50f, decExtents.z ),   new Vector3 ( -decExtents.x, ( decExtents.y * 2 ) + 50f, decExtents.z ), 
                                                   new Vector3 ( -decExtents.x, ( decExtents.y * 2 ) + 50f, -decExtents.z ), new Vector3 ( decExtents.x, ( decExtents.y * 2 ) + 50f, -decExtents.z ) };
            
            do
            {                               
                // Random location with constraints derived from the base object's mesh extents
                location = new Vector3( Random.Range( -baseExtents.x + decExtents.x, baseExtents.x - decExtents.x ), 0f, Random.Range( -baseExtents.z + decExtents.z, baseExtents.z - decExtents.z ) );
                
                // Casts 5 rays downwards from the center of the location and each of the four corners to check the local height of the base object mesh
                for ( int h = 0; h < 5; h++ )
                {
                    Ray downRay = new Ray( location + modifiers[h] , -Vector3.up );

                    // Saves height data from each raycast to an array
                    if ( Physics.Raycast(downRay, out hit) )
                    {
                        if ( hit.collider.name != "BoxCollider" ) // The colliders added to the previous decorator objects are named "BoxCollider"
                            yCoordsFrmRaycast[h] = hit.point.y;
                        else
                            yCoordsFrmRaycast[h] = hit.point.y * ( h + 1 ) * 100; // Sets null value to ensure the decorator won't spawn on top of another decorator's temporary BoxCollider
                    }
                    else
                        Debug.LogWarning( "Error with base object's mesh data" );
                }

            }
            while ( FindRange( ref yCoordsFrmRaycast ) > tolerance );

            // Take average of all surrounding environment y coords for the decorators y coord
            location.y = ( yCoordsFrmRaycast[0] + yCoordsFrmRaycast[1] + yCoordsFrmRaycast[2] + yCoordsFrmRaycast[3] + yCoordsFrmRaycast[4] ) / 5f;
            
            // Moves decorator to selected location
            decorator.transform.position = location;

            // Set decorator as child to the new slice being made
            decorator.transform.SetParent( currentObject.transform, true );

            // Set custom name with number for each decorator
            decorator.name = i + "_" + listTwo[randomIndex].name;

            // Add a box collider around the full bounding box of the decorator so it will trigger the raycasts and avoid overlapping decorators
            bc[i] = decorator.AddComponent("BoxCollider") as BoxCollider;
        }

        // After all decorators have been created, remove temporary mesh collider from base object
        DestroyImmediate( mc );

        // Remove all temporary box colliders from decorators
        for ( int i = 0; i < bc.Length; i++ )
            DestroyImmediate( bc[i] );
    }

    // Finds the highest and lowest height values and returns the range
    float FindRange ( ref float[] array )
    {
        float highest = array[0];
        float lowest  = array[0];

        for ( int i = 0; i < array.Length; i++ )
        {
            if ( array[i] > highest )
                highest = array[i];
            if ( array[i] < lowest )
                lowest = array[i];
        }

        return highest - lowest;
    }

    // Saves current full object as a prefab in designated folder and removes original
    void FinalizePrefab ()
    {
        currentObject = GameObject.Find( currentName );

        string localPath = "Assets/" + prefabFolder + "/" + currentObject.name + ".prefab";
        
        // Checks if prefab of same name already exists--if so, asks for overwrite confirmation from user
        if ( AssetDatabase.LoadAssetAtPath( localPath, typeof(GameObject) ) )
        {
            if ( !EditorUtility.DisplayDialog("Warning", "'" + currentObject.name + "' already exists. Overwrite?", "Yes", "No"))
                return;
        }

        // Create designated folder if it doesn't already exist
        if ( !Directory.Exists( Application.dataPath + "/" + prefabFolder ) )
                    AssetDatabase.CreateFolder( "Assets", prefabFolder );

        // Creates and fills the prefab
        Object emptyPrefab = PrefabUtility.CreateEmptyPrefab( localPath );
        PrefabUtility.ReplacePrefab( currentObject, emptyPrefab, ReplacePrefabOptions.ConnectToPrefab );

        AssetDatabase.Refresh();

        // Delete original
        DestroyImmediate( currentObject );

        // Increment counter used for naming convention
        prefabCounter++;
    }

}
