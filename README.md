# ABB-Assembly-MR-Demo

This application is a dual-platform industrial XR assembly training tool built for the ABB 
OT200E03 200A three-pole switch disconnector (IEC 60947-3). It allows factory workers 
to practise disassembling and reassembling all 45 components of the physical switch in 
an augmented or mixed reality environment before working on the actual hardware.

The application runs on two platforms from a single Unity project: 

*Mixed Reality (Meta Quest 3):* 

The user wears the headset and sees the real world 
through passthrough cameras. Virtual switch components are spawned inside the 
highlighted areas on a detected real surface, i.e., a table or floor, when the grip button of 
the right controller is pressed. The user picks up parts using the trigger button of the 
controllers and assembles them in a fixed virtual assembly zone visible through a ghost 
mesh overlay system. 

*Augmented Reality (Android Mobile):* 

The user holds their phone and points the back 
camera at a flat surface. Parts are spawned inside the highlighted areas on a detected 
real surface. Parts can be picked and placed using the touchscreen. 
Both platforms share all assembly logic, prerequisite rules, visual guide system, physics 
configuration, hint system, and part label system. Only the input handling and platform
specific XR session management differ between scenes. 

The switch has 45 physical components organised into five groups: 
• Driver Module: the primary housing containing the operating shaft, interlocking 
nuts, internal fasteners, top cover, red and white strips, shaft collar, clamping bolt, 
external fasteners, and mounting clips (18 parts) 
• Receiver Module 1: right and left housing shells, interlocking drive ring, contact 
cover, contact window, and two terminal lugs (7 parts) 
• Receiver Module 2: identical structure to Receiver Module 1 (7 parts) 
• Receiver Module 3: identical structure to Receiver Modules 1 and 2, plus external 
fasteners and mounting clips (11 parts) 
• Modular Rods: two long rods that thread through all four modules and fasten the 
complete assembly (2 parts)
