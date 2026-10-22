import { useEffect, useId, useRef } from "react";

export default function Modal({ title, onClose, children, wide = false }) {
    const titleId = useId();
    const dialogRef = useRef(null);
    const onCloseRef = useRef(onClose);

    useEffect(() => {
        onCloseRef.current = onClose;
    });

    useEffect(() => {
        const previouslyFocused = document.activeElement;

        const onKeyDown = (event) => {
            if (event.key === "Escape") onCloseRef.current();
        };

        document.addEventListener("keydown", onKeyDown);
        dialogRef.current?.focus();

        return () => {
            document.removeEventListener("keydown", onKeyDown);
            previouslyFocused?.focus?.();
        };
    }, []);

    return (
        <div
            className="modal-backdrop"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) onClose();
            }}
        >
            <div
                ref={dialogRef}
                className={wide ? "modal modal-wide" : "modal"}
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
                tabIndex={-1}
            >
                <div className="modal-head">
                    <h2 id={titleId}>{title}</h2>
                    <button type="button" className="close-btn" onClick={onClose} aria-label="Close dialog">
                        &times;
                    </button>
                </div>
                <div className="modal-body">{children}</div>
            </div>
        </div>
    );
}