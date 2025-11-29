/**
 * Arduino Glue Controller - Main Entry Point
 * 
 * This file contains the main Arduino setup() and loop() functions.
 * The actual implementation is in GlueController.cpp
 */

#include "GlueController.h"

// The setup() function runs once when the board starts up
void setup() {
  controllerSetup();
}

// The loop() function runs continuously after setup()
void loop() {
  controllerLoop();
}
