console.log("WebDesignGallery script loaded");

// Ensure React is available (it should be, based on your app.razor file)
if (typeof React === 'undefined' || typeof ReactDOM === 'undefined') {
    console.error("❌ React or ReactDOM is not loaded. Check your script order in app.razor.");
} else {
    console.log("✅ React and ReactDOM found.");
}

function WebDesignGallery() {
    const projects = [
        {
            id: 1,
            title: "Modern Landing Page",
            description: "A sleek, responsive landing page concept.",
            // Using placeholder image URLs since the originals are local paths
            image: "https://placehold.co/400x300/4c4c4c/ffffff?text=Landing+Page",
            figmaUrl: "https://www.figma.com/file/EXAMPLE1"
        },
        {
            id: 2,
            title: "E-commerce UI",
            description: "Minimalist product grid and checkout flow.",
            image: "https://placehold.co/400x350/1e293b/ffffff?text=E-commerce+UI",
            figmaUrl: "https://www.figma.com/file/EXAMPLE2"
        },
        {
            id: 3,
            title: "Portfolio Website",
            description: "Creative portfolio layout for designers.",
            image: "https://placehold.co/400x450/0f766e/ffffff?text=Portfolio",
            figmaUrl: "https://www.figma.com/file/EXAMPLE3"
        },
        {
            id: 4,
            title: "Dashboard UI",
            description: "An analytics dashboard for startups.",
            image: "https://placehold.co/400x320/94a3b8/ffffff?text=Dashboard",
            figmaUrl: "https://www.figma.com/file/EXAMPLE4"
        }
    ];

    // Use pure JavaScript (React.createElement) instead of JSX
    const projectElements = projects.map(p => {
        // Create the 'View in Figma' link
        const figmaLink = React.createElement("a", {
            href: p.figmaUrl,
            className: "figma-btn",
            target: "_blank",
            rel: "noopener noreferrer",
            style: { 
                display: 'inline-block', 
                marginTop: '10px', 
                padding: '8px 16px', 
                backgroundColor: '#10b981', // Emerald 500
                color: 'white', 
                borderRadius: '6px', 
                textDecoration: 'none', 
                fontWeight: '600' 
            }
        }, "View in Figma");

        // Create the card element.
        return React.createElement("div", {
            className: "web-card",
            key: p.id,
            style: {
                backgroundColor: 'white',
                borderRadius: '12px',
                boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
                padding: '20px ',
                // --- 1. SET FIXED HEIGHT FOR UNIFORM CARDS ---
                height: '450px', 
                width: '600px',
                // ---------------------------------------------
                marginBottom: '20px',
                transition: 'transform 0.2s',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center', // Centers contents horizontally (image, text, button)
                textAlign: 'center'   // Centers text
            }
        },
            // --- 2. IMAGE CONTAINER FOR FIXED ASPECT RATIO ---
            React.createElement("div", {
                className: "image-container",
                style: {
                    width: '100%', 
                    height: '300px', // Fixed height for image area
                    overflow: 'hidden',
                    marginBottom: '15px',
                    borderRadius: '8px',
                    display: 'flex', 
                    justifyContent: 'center', 
                    alignItems: 'center' 
                }
            },
                // Image element
                React.createElement("img", {
                    src: p.image,
                    alt: p.title,
                    style: { 
                        width: '100%', 
                        // Use object-fit cover to ensure image fills container without distortion
                        objectFit: 'cover', 
                        height: '100%', 
                    }
                })
            ),
            // -------------------------------------------------
            
            // Title
            React.createElement("h3", { 
                style: { 
                    fontSize: '1.25rem', 
                    fontWeight: '700', 
                    margin: '0 0 5px 0',
                    // --- 3. PUSH CONTENT TO THE BOTTOM ---
                    marginTop: 'auto' // This pushes the title (and everything below it) down
                    // -------------------------------------
                } 
            }, p.title),
            // Description
            React.createElement("p", { 
                style: { 
                    color: '#6b7280', 
                    fontSize: '0.9rem', 
                    marginBottom: '10px' 
                } 
            }, p.description),
            // Link
            figmaLink
        );
    });

    // Return the main container grid
    return React.createElement("div", {
        className: "web-grid",
        style: {
            display: 'grid',
            // Enforce exactly two columns (2, 1fr)
            gridTemplateColumns: 'repeat(2, 1fr)',
            gap: '20px',
            padding: '20px',
            maxWidth: '1200px',
            margin: '0 auto',
           
        }
    }, projectElements);
}

// Expose globally so Blazor can find it
window.renderWebDesignGallery = function (rootId) {
    console.log("🚀 renderWebDesignGallery fired for:", rootId);
    const container = document.getElementById(rootId);
    
    if (!container) {
        console.error("❌ No element with id:", rootId);
        return;
    }

    // React 18 way: Use createRoot (This is the modern, preferred method)
    if (window.ReactDOM && ReactDOM.createRoot) {
        // Use a property on the container to store the root instance
        if (!container._reactRoot) {
             container._reactRoot = ReactDOM.createRoot(container);
        }
        
        // Render the component using React.createElement to pass the component reference
        container._reactRoot.render(React.createElement(WebDesignGallery));
        console.log("✅ React component rendered using createRoot.");
    } else {
        console.error("❌ ReactDOM.createRoot not found. Ensure React 18 is loaded.");
    }
};