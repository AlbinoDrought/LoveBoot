# LoveBoot

LoveBoot is an automatic [LoveBeat](http://store.steampowered.com/app/354290/) player, similar to a cheat. It should also support [LoveRitmo](http://loveritmo.softnyx.com/), [LoveBeat Korea](http://lovebeat.plaync.com/), and other region-specific versions of the same game.

Uses Emgu/OpenCV image recognition to recognize the gamestate (keys), and when to fulfill the gamestate (bars).

LoveBoot does not access or alter the game's memory in any way - it is not a hack. Despite this, you may still get banned. LoveBoot does not employ any form of anti-cheat bypass. 

If you have issues running after compiling, see [this link](http://stackoverflow.com/questions/503427/the-type-initializer-for-emgu-cv-cvinvoke-threw-an-exception) (copy emgu dlls to folder)

### Usage

  - [F7] Toggle overlay
  - [F8] Toggle auto-ready (Presses F5 to ready after ~40s of no keys being detected)
  - [F9] Toggle 8-key or 4-key mode
  - [F10] Toggle bot (on or off)

### Videos

  - [Normal 4-key](https://www.youtube.com/watch?v=BzrZuJprFVY)
  - [Normal 8-key](https://www.youtube.com/watch?v=F7P7MitfqPE)
  - [Random 8-key](https://www.youtube.com/watch?v=jQutaLH6nrc)
  - [LoveRitmo 330BPM 4-key](https://www.youtube.com/watch?v=g7YFK9YhmCs)

### References
Relies on:

  - [Emgu](http://www.emgu.com/wiki/index.php/Main_Page)
  - [Input Simulator](http://inputsimulator.codeplex.com/)
  - *Stack Overflow posts referenced above their respective usages.*


