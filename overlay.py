import tkinter as tk
from tkinter import filedialog
import os

try:
    from PIL import Image, ImageTk
    HAS_PIL = True
except ImportError:
    HAS_PIL = False

class OverlayApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Image Overlay")
        self.root.attributes('-topmost', True)
        self.root.overrideredirect(True) # Borderless window
        self.root.geometry("400x300")
        
        self.opacity = 0.8
        self.root.attributes('-alpha', self.opacity)
        
        self.image = None
        self.photo = None
        
        # UI Elements
        self.canvas = tk.Canvas(root, bg='black', highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)
        
        # Controls Frame
        self.control_frame = tk.Frame(root, bg='#333333')
        self.control_frame.place(relx=0.5, rely=0.9, anchor=tk.CENTER)
        
        self.load_btn = tk.Button(self.control_frame, text="Load Image", command=self.load_image, bg='#555555', fg='white', relief=tk.FLAT)
        self.load_btn.pack(side=tk.LEFT, padx=5, pady=5)
        
        self.opacity_scale = tk.Scale(self.control_frame, from_=0.1, to=1.0, resolution=0.05, 
                                      orient=tk.HORIZONTAL, command=self.update_opacity, 
                                      bg='#333333', fg='white', highlightthickness=0, length=150)
        self.opacity_scale.set(self.opacity)
        self.opacity_scale.pack(side=tk.LEFT, padx=5, pady=5)
        
        self.close_btn = tk.Button(self.control_frame, text="Close", command=root.quit, bg='#cc0000', fg='white', relief=tk.FLAT)
        self.close_btn.pack(side=tk.LEFT, padx=5, pady=5)
        
        # Bindings for moving the borderless window
        self.canvas.bind("<ButtonPress-1>", self.start_move)
        self.canvas.bind("<B1-Motion>", self.do_move)
        
        # Allow exiting with Escape key
        self.root.bind("<Escape>", lambda e: root.quit())
        
        if not HAS_PIL:
            self.canvas.create_text(200, 150, text="Please install Pillow to load various image formats.\nRun: pip install Pillow", 
                                    fill="white", justify=tk.CENTER)
        else:
            self.canvas.create_text(200, 150, text="Click 'Load Image' to start.\nDrag to move.\nPress 'Esc' to close.", 
                                    fill="white", justify=tk.CENTER, tags="instruction")

    def load_image(self):
        filetypes = [("Image Files", "*.png;*.jpg;*.jpeg;*.gif;*.bmp")]
        if not HAS_PIL:
            filetypes = [("GIF/PGM/PPM Files", "*.gif;*.pgm;*.ppm")]
            
        filepath = filedialog.askopenfilename(filetypes=filetypes)
        if filepath:
            self.show_image(filepath)
            
    def show_image(self, filepath):
        if HAS_PIL:
            self.image = Image.open(filepath)
            width, height = self.image.size
            self.photo = ImageTk.PhotoImage(self.image)
        else:
            self.photo = tk.PhotoImage(file=filepath)
            width = self.photo.width()
            height = self.photo.height()
            
        # Ensure window doesn't get larger than screen
        screen_width = self.root.winfo_screenwidth()
        screen_height = self.root.winfo_screenheight()
        
        if width > screen_width or height > screen_height:
            if HAS_PIL:
                self.image.thumbnail((screen_width - 100, screen_height - 100), Image.Resampling.LANCZOS)
                width, height = self.image.size
                self.photo = ImageTk.PhotoImage(self.image)
        
        self.root.geometry(f"{width}x{height}")
        self.canvas.delete("all")
        self.canvas.create_image(0, 0, anchor=tk.NW, image=self.photo)
        
        # Bring controls to front
        self.control_frame.lift()
        self.control_frame.place(relx=0.5, rely=0.9, anchor=tk.CENTER)
            
    def update_opacity(self, value):
        self.opacity = float(value)
        self.root.attributes('-alpha', self.opacity)

    def start_move(self, event):
        self.x = event.x
        self.y = event.y

    def do_move(self, event):
        deltax = event.x - self.x
        deltay = event.y - self.y
        x = self.root.winfo_x() + deltax
        y = self.root.winfo_y() + deltay
        self.root.geometry(f"+{x}+{y}")

if __name__ == "__main__":
    root = tk.Tk()
    app = OverlayApp(root)
    root.mainloop()
