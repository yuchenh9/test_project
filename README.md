
![Image 2025-01-06 at 2 38 PM](https://github.com/user-attachments/assets/9d7d9d13-ebdd-4eaf-8f56-fc217334d5ec)

Here you should see plants,controller, canvas, fryingPan and others.

### **Plants**


"plants" object is a list of the vegetables that can be displayed in the game, 
as shown below, a vegetable parent object has "axises" object and "object" object.

<img width="293" alt="Screenshot 2024-12-08 at 11 12 59 PM" src="https://github.com/user-attachments/assets/123fe485-5d44-471e-852c-7436c6b84e1c">

and the axises object stores three pair of spheres, which are used to represent the cutting axises (to cut the vegetable in horizontal, vertical, and z --three directions), as shown below

<img width="669" alt="Screenshot 2024-12-08 at 11 14 06 PM" src="https://github.com/user-attachments/assets/e4c9b93a-5d86-404f-b73d-c761bfec9680">

### **canvas**

the second important object is "canvas" object, it displays the user interface, the minus plus and xyz buttons.


<img width="387" alt="Screenshot 2024-12-08 at 11 28 24 PM" src="https://github.com/user-attachments/assets/e74d7556-4990-4c87-ac77-abbbdc4fec3e">
<img width="409" alt="Screenshot 2024-12-08 at 11 32 55 PM" src="https://github.com/user-attachments/assets/0e1d0d41-5327-456a-8da7-9bc8414ea221">

the click handler script is not attached to each button, instead, all clicks are handled by a UnifiedButtonHandler under the Canvas object

<img width="543" alt="Screenshot 2024-12-08 at 11 36 27 PM" src="https://github.com/user-attachments/assets/a5368e95-99a6-4abc-945b-e6ec9be02f51">

<img width="561" alt="Screenshot 2024-12-08 at 11 47 36 PM" src="https://github.com/user-attachments/assets/874a49b5-24b3-424b-a326-2f96b429e2cc">
the "tomato" object in tomato_parent/object has important sctipts attached to it.
1. the Layer is set as "outline"
<img width="191" alt="Screenshot 2024-12-08 at 11 51 13 PM" src="https://github.com/user-attachments/assets/dbc7058f-c474-48df-baeb-206f8c073c0c">
it is a custome layer that was added, it the object is set to default, the outline will not render. 
