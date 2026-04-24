# 3D Procedurally Generated Caves

https://github.com/user-attachments/assets/2f82435e-d64d-4990-a65b-a47f8b6d8849

## How To Play
### Gameplay
WASD to move. 

Space to jump.

Mouse to aim.

Click on a tile to mine.

Right click to explode nearby tiles. 

### Editor
Make sure you are in the 'MainScene' scene. Click play to start the game.

If you want to generate caves in the editor, select the 'RoomGenerator' object, and click the buttons at the bottom of the inspector. Adjust the step time to visualise the generation process.

## How It Works
First a 2D mask is created using a variety of algorithms such as
- Random Walker Algorithm
- Cellular Automata
- Flood Fill

The floor and ceiling then has height added using a distance-field from the edge combined with perlin noise.

Next the mesh is then build using voxels by looping through each tile and checking its faces.

Finally weighted tiles are randomly placed along the caves floor using perlin noise.

## Further Info
https://www.youtube.com/watch?v=La4kfoMZxok (Me)

https://www.youtube.com/playlist?list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9 (Sebastian Lague)
