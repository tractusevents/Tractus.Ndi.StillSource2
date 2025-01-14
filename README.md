# Tractus Still Source Generator 2 for NDI

This is a replacement tool for my original *Still Source Generator for NDI*, found here: https://agfinn.gumroad.com/l/ndi-still-source-generator

This utility allows you to load up PNG or JPG stills and send them out as NDI sources.

## Differences from the NDI Test Pattern App

- You can mimic Test Pattern mode (which sends 1 frame per second), or send out actual realized frames
  * E.g. if your frame rate is 60 fps, in regular mode the app will send the still out 60 times per second
- SDR-only for the moment
- Cross-platform (Linux and Windows planned, Mac might happen later)