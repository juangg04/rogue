using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap tilemap;                   // Tilemap donde colocar los tiles
    public TileBase backgroundTile;           // Tile para el fondo
    public TileBase squareTile;               // Tile para los cuadrados
    public TileBase pasillo;                  // Tile para el pasillo
    public int mapWidth = 100;                // Ancho del mapa
    public int mapHeight = 100;               // Alto del mapa
    public int minSquareSize = 5;             // Tamaño mínimo de los cuadrados
    public int maxSquareSize = 10;            // Tamaño máximo de los cuadrados
    public int minSeparation = 3;             // Mínima separación entre cuadrados

    private int squareCount;

    void Start()
    {
        squareCount = Random.Range(3, 7);
        GenerateBackground();
        GenerateSquares();
    }

    void GenerateBackground()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePosition, backgroundTile);  // Coloca el tile de fondo
            }
        }
        Debug.Log("Mapa de 100x100 generado con fondo.");
    }

    void GenerateSquares()
    {
        List<RectInt> placedSquares = new List<RectInt>();  // Lista para almacenar los cuadrados colocados

        for (int i = 0; i < squareCount; i++)
        {
            bool squarePlaced = false;
            int attempts = 0;

            while (!squarePlaced && attempts < 100)
            {
                // Genera tamaño y posición aleatoria del cuadrado
                int squareWidth = Random.Range(minSquareSize, maxSquareSize + 1);
                int squareHeight = Random.Range(minSquareSize, maxSquareSize + 1);

                // Asegura que el cuadrado esté dentro de los límites y a 1 tile del borde
                int startX = Random.Range(5, mapWidth - squareWidth - 1);
                int startY = Random.Range(5, mapHeight - squareHeight - 1);

                // Crea un rectángulo que representa el área del cuadrado con separación
                RectInt squareArea = new RectInt(startX, startY, squareWidth, squareHeight);
                RectInt expandedArea = new RectInt(startX - minSeparation, startY - minSeparation, squareWidth + minSeparation * 2, squareHeight + minSeparation * 2);

                // Verifica que el área expandida no colisione con otros cuadrados ya colocados
                bool overlap = false;
                foreach (RectInt existingSquare in placedSquares)
                {
                    if (expandedArea.Overlaps(existingSquare))
                    {
                        overlap = true;
                        break;
                    }
                }

                // Si no hay solapamiento, coloca el cuadrado
                if (!overlap)
                {
                    List<Vector3Int> passageStartPoints = new List<Vector3Int>();
                    PlaceSquare(squareArea, passageStartPoints);

                    // Ahora `passageStartPoints` contiene 1 a 3 puntos de inicio para los pasillos
                    foreach (var startPoint in passageStartPoints)
                    {
                        Debug.Log($"Punto de inicio de pasillo: {startPoint}");
                        CreateCorridor(startPoint);
                    }

                    placedSquares.Add(squareArea);
                    squarePlaced = true;
                }

                attempts++;
            }
        }

        Debug.Log(placedSquares.Count);
    }

    void PlaceSquare(RectInt area, List<Vector3Int> passageStartPoints)
    {
        List<Vector3Int> borderTiles = new List<Vector3Int>();  // Lista temporal para almacenar los bordes de este cuadrado

        // Rellena el área del cuadrado y detecta los bordes
        for (int x = area.xMin; x < area.xMax; x++)
        {
            for (int y = area.yMin; y < area.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePosition, squareTile);  // Coloca el tile específico para el cuadrado

                // Verifica si la posición es un borde del cuadrado y excluye las esquinas
                bool isBorder = (x == area.xMin || x == area.xMax - 1 || y == area.yMin || y == area.yMax - 1);
                bool isCorner = (x == area.xMin && y == area.yMin) ||
                                (x == area.xMin && y == area.yMax - 1) ||
                                (x == area.xMax - 1 && y == area.yMin) ||
                                (x == area.xMax - 1 && y == area.yMax - 1);

                if (isBorder && !isCorner)
                {
                    borderTiles.Add(tilePosition);  // Agrega el borde a la lista de bordes excluyendo esquinas
                }
            }
        }

        // Selecciona entre 1 y 3 tiles de borde al azar para pasillos
        int passageCount = Random.Range(1, 4);  // Número de pasillos a crear (1 a 3)
        for (int i = 0; i < passageCount; i++)
        {
            if (borderTiles.Count == 0) break;  // Asegúrate de que haya bordes restantes para elegir

            int randomIndex = Random.Range(0, borderTiles.Count);
            Vector3Int selectedTile = borderTiles[randomIndex];

            // Verificar si el tile seleccionado está pegado a algún pasillo ya colocado
            bool isAdjacent = false;
            foreach (var passage in passageStartPoints)
            {
                if (Mathf.Abs(selectedTile.x - passage.x) <= 1 && Mathf.Abs(selectedTile.y - passage.y) <= 1)
                {
                    isAdjacent = true;
                    break;
                }
            }

            // Solo agrega el tile si no está pegado a otro pasillo
            if (!isAdjacent)
            {
                passageStartPoints.Add(selectedTile);
                borderTiles.RemoveAt(randomIndex);
            }
        }


        Debug.Log($"Cuadrado colocado en {area.position} de tamaño {area.size}");
    }

    void CreateCorridor(Vector3Int startPoint)
    {
        int direccion = 0;
        // 0 parado, 1 derecha, 2 izquierda, 3 arriba, 4 abajo

        TileBase tileArriba = tilemap.GetTile(startPoint + new Vector3Int(0, 1, 0));
        TileBase tileAbajo = tilemap.GetTile(startPoint + new Vector3Int(0, -1, 0));
        TileBase tileDerecha = tilemap.GetTile(startPoint + new Vector3Int(1, 0, 0));
        TileBase tileIzquierda = tilemap.GetTile(startPoint + new Vector3Int(-1, 0, 0));

        tilemap.SetTile(startPoint, pasillo);  // Marca el punto de inicio como pasillo

        if (tileArriba == backgroundTile)
        {
            Debug.Log("Casilla: " + startPoint + "Hacia arriba.");
        }
        else if (tileAbajo == backgroundTile)
        {
            Debug.Log("Casilla: " + startPoint + "Hacia abajo.");
        }
        else if (tileDerecha == backgroundTile)
        {
            Debug.Log("Casilla: " + startPoint + "Hacia la derecha.");
        }
        else if (tileIzquierda == backgroundTile)
        {
            Debug.Log("Casilla: " + startPoint + "Hacia la izquierda.");
        }
    }
}