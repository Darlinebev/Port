

window.reactInterop = {
    renderGallery: function (elementId) {
        const { useState, useEffect } = React;

        function Gallery() {
            const [images, setImages] = useState([]);
            const [selected, setSelected] = useState(null);

            useEffect(() => {
                fetch("/api/graphics-images")
                    .then(res => res.json())
                    .then(data => setImages(data.map((url, idx) => ({ id: idx + 1, url }))))
                    .catch(err => console.error("Failed to load images", err));
            }, []);

            return React.createElement("div", {},
                // Masonry-like grid
                React.createElement("div", {
                    style: {
                        columnCount: 4,
                        columnGap: "15px",
                        padding: "20px",
                        maxWidth: "1200px",
                        margin: "0 auto"
                    }
                },
                    images.map(img =>
                        React.createElement("div", {
                            key: img.id,
                            style: {
                                display: "inline-block",
                                width: "100%",
                                marginBottom: "15px",
                                cursor: "pointer",
                                background: "#fff",
                                borderRadius: "10px",
                                overflow: "hidden",
                                boxShadow: "0 6px 16px rgba(0, 0, 0, 0.08)",
                                transition: "transform 0.3s, box-shadow 0.3s"
                            },
                            onClick: () => setSelected(img)
                        },
                            React.createElement("img", {
                                src: img.url,
                                alt: "",
                                style: {
                                    width: "100%",
                                    display: "block",
                                    borderRadius: "10px",
                                    transition: "transform 0.3s"
                                }
                            })
                        )
                    )
                ),

                // Fullscreen overlay
                selected && React.createElement("div", {
                    onClick: () => setSelected(null),
                    style: {
                        position: "fixed",
                        top: 0,
                        left: 0,
                        width: "100%",
                        height: "100%",
                        backgroundColor: "rgba(0,0,0,0.8)",
                        display: "flex",
                        justifyContent: "center",
                        alignItems: "center",
                        zIndex: 1000,
                        cursor: "pointer"
                    }
                },
                    React.createElement("img", {
                        src: selected.url,
                        alt: "",
                        style: { maxWidth: "90%", maxHeight: "90%", borderRadius: "8px" }
                    }),
                    React.createElement("button", {
                        onClick: (e) => { e.stopPropagation(); setSelected(null); },
                        style: {
                            position: "absolute",
                            top: "20px",
                            right: "40px",
                            background: "#fff",
                            border: "none",
                            borderRadius: "5px",
                            padding: "10px 20px",
                            cursor: "pointer",
                            fontWeight: "bold",
                            fontSize: "16px"
                        }
                    }, "X")
                )
            );
        }

        ReactDOM.render(React.createElement(Gallery), document.getElementById(elementId));
    }
};
