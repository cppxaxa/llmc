import tkinter as tk
import math

def calculate():
    try:
        expression = entry.get()
        result = eval(expression)
        entry.delete(0, tk.END)
        entry.insert(0, result)
    except (SyntaxError, NameError, ZeroDivisionError):
        entry.delete(0, tk.END)
        entry.insert(0, "Error")

def add_to_entry(value):
    entry.insert(tk.END, value)

def clear_entry():
    entry.delete(0, tk.END)


# ---  Tkinter Setup ---
root = tk.Tk()
root.title("Elegant Calculator")
root.geometry("300x400")  # Adjust window size
root.configure(bg="#f0f0f0") #light grey background


# --- Styling ---
button_style = {
    "bg": "#4CAF50",  # Green
    "fg": "white",
    "font": ("Helvetica", 16),
    "width": 5,
    "height": 2,
    "relief": tk.GROOVE,
    "activebackground": "#45a049", #darker green on hover
    "bd":2 #border width
}


entry = tk.Entry(root, font=("Helvetica", 24), justify="right", borderwidth=3, relief="sunken", bg="white")
entry.grid(row=0, column=0, columnspan=4, padx=10, pady=10, sticky="nsew")


buttons = [
    '7', '8', '9', '/',
    '4', '5', '6', '*',
    '1', '2', '3', '-',
    '0', '.', '=', '+',
    'pow', 'sqrt', 'C'
]

row = 1
col = 0
for button_text in buttons:
    if button_text == '=':
        button = tk.Button(root, text=button_text, command=calculate, **button_style)
    elif button_text == 'C':
        button = tk.Button(root, text=button_text, command=clear_entry, **button_style)
    elif button_text == 'pow':
        button = tk.Button(root, text=button_text, command=lambda: add_to_entry('**'), **button_style)
    elif button_text == 'sqrt':
        button = tk.Button(root, text=button_text, command=lambda: add_to_entry('math.sqrt('), **button_style)
    else:
        button = tk.Button(root, text=button_text, command=lambda text=button_text: add_to_entry(text), **button_style)
    button.grid(row=row, column=col, padx=5, pady=5, sticky="nsew")
    col += 1
    if col > 3:
        col = 0
        row += 1


# configure column weights for resizing
for i in range(4):
    root.columnconfigure(i, weight=1)
for i in range(len(buttons)//4+1):
    root.rowconfigure(i, weight=1)
