When reproducing and testing the game, make sure you use the iphone simulator in play mode!
set an axis (x,y,xyz...) and click the plus(+) or minus(-) button to cut the tomato.

<img width="567" alt="Screenshot 2025-01-09 at 8 43 10 AM" src="https://github.com/user-attachments/assets/5ce2b251-72fc-4f9c-9e7f-bb94abb82de9" />

### **1. Main Scene Hierarchy**

![Image 2025-01-06 at 2 38 PM](https://github.com/user-attachments/assets/9d7d9d13-ebdd-4eaf-8f56-fc217334d5ec)

Here you should see plants,controller, canvas, fryingPan and others.

### **1.1 Plants**


"plants" object is a list of the vegetables that can be displayed in the game, 
as shown below, a vegetable parent object has "axises" object and "object" object.

<img width="293" alt="Screenshot 2024-12-08 at 11 12 59 PM" src="https://github.com/user-attachments/assets/123fe485-5d44-471e-852c-7436c6b84e1c">

and the axises object stores three pair of spheres, which are used to represent the cutting axises (to cut the vegetable in horizontal, vertical, and z --three directions), as shown below

<img width="669" alt="Screenshot 2024-12-08 at 11 14 06 PM" src="https://github.com/user-attachments/assets/e4c9b93a-5d86-404f-b73d-c761bfec9680">

There are some important things about the tomato object to be noticed.

### **1.1.1.**

the Layer is set as "outline"
   
 <img width="561" alt="Screenshot 2024-12-08 at 11 47 36 PM" src="https://github.com/user-attachments/assets/874a49b5-24b3-424b-a326-2f96b429e2cc">

<img width="191" alt="Screenshot 2024-12-08 at 11 51 13 PM" src="https://github.com/user-attachments/assets/dbc7058f-c474-48df-baeb-206f8c073c0c">

it is a custome layer that was added, if the layer of the object is set to default, the outline shader will not render. 

### **1.1.2.**

the material of the tomato is tomato_material, which is using a simple_outline_body_shader shader graph.
   
The shaders in this project are located in Assets/Resources2/shadersFolder

the simple_outline_body_shader looks like below
<img width="705" alt="Screenshot 2025-01-08 at 9 27 15 AM" src="https://github.com/user-attachments/assets/4043d6c6-c73c-4555-8f82-53a76ed96dc8" />

### **1.1.3.**

A MeshTarget script is attached to it. This is the script that enables an object to be cut. The script is located in Assets/DynamicMeshCutter/Scripts/Utility.
The face material is selected in this script, and it is the material used for the cut surface that is newly created. The face material needs the same shader as the object material, otherwise it looks very weird.

### **1.2 canvas**

The second important object is "canvas" object, it displays the user interface, the minus plus and xyz buttons.


<img width="387" alt="Screenshot 2024-12-08 at 11 28 24 PM" src="https://github.com/user-attachments/assets/e74d7556-4990-4c87-ac77-abbbdc4fec3e">
<img width="409" alt="Screenshot 2024-12-08 at 11 32 55 PM" src="https://github.com/user-attachments/assets/0e1d0d41-5327-456a-8da7-9bc8414ea221">

The click handler script is not attached to each button, instead, all clicks are handled by a UnifiedButtonHandler under the Canvas object

Each time the number is changed as you click on the minus or plus button, it makes a new cut.

when "x" "y" "z" are clicked, the game sets the cutting axis to x or y or z, 

when "xy" is clicked, it sets two cutting axises, x and y

when "cir" is clicked, it sets circular cutting axises like cuting a pizze.

all those buttons are controlled by onclick functions in the UnifiedButtonHandler script in the Assets/Scripts folder

<img width="543" alt="Screenshot 2024-12-08 at 11 36 27 PM" src="https://github.com/user-attachments/assets/a5368e95-99a6-4abc-945b-e6ec9be02f51">



