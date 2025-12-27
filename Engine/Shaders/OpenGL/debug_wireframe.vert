
        
        #version 330 core
        
        layout(location = 0) in vec3 position;
        uniform mat4 viewProjection;
        uniform vec4 color;
        out vec4 vColor;
        
        void main()
        {
            gl_Position = viewProjection * vec4(position, 1.0);
            vColor = color;
        }
        
        
  
