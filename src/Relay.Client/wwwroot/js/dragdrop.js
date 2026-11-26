(() => {
let dotNetRef = null;
let draggedId = null;
let draggedCard = null;
let dragImage = null;

window.relayDrag = {
    start(ref) {
        if (dotNetRef) {
            return;
        }

        dotNetRef = ref;
        document.addEventListener("dragstart", onDragStart);
        document.addEventListener("dragover", onDragOver);
        document.addEventListener("drop", onDrop);
        document.addEventListener("dragend", onDragEnd);
    },
    stop() {
        document.removeEventListener("dragstart", onDragStart);
        document.removeEventListener("dragover", onDragOver);
        document.removeEventListener("drop", onDrop);
        document.removeEventListener("dragend", onDragEnd);
        clearDrag();
        dotNetRef = null;
    }
};

function onDragStart(event) {
    const target = elementFrom(event.target);
    const card = target?.closest("[data-card-id]");
    if (!card || !card.closest("[data-board]")) {
        return;
    }

    if (target?.closest("button, input, textarea, a, form")) {
        event.preventDefault();
        return;
    }

    draggedId = card.dataset.cardId;
    draggedCard = card;
    card.classList.add("dragging");
    dragImage = card.cloneNode(true);
    dragImage.classList.add("drag-image");
    dragImage.style.position = "absolute";
    dragImage.style.top = "-1000px";
    dragImage.style.left = "0";
    dragImage.style.width = card.offsetWidth + "px";
    document.body.appendChild(dragImage);
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", draggedId);
    event.dataTransfer.setDragImage(dragImage, event.offsetX, event.offsetY);
}

function onDragOver(event) {
    const column = columnFrom(event.target);
    if (!column || !draggedId) {
        return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    placeSlot(column, event.clientY, draggedId);
}

async function onDrop(event) {
    const column = columnFrom(event.target);
    const cardId = draggedId;
    if (!column || !cardId || !dotNetRef) {
        clearDrag();
        return;
    }

    event.preventDefault();
    const columnId = column.dataset.columnId;
    const index = dropIndex(column, event.clientY, cardId);
    clearDrag();

    try {
        await dotNetRef.invokeMethodAsync("OnCardDropped", cardId, columnId, index);
    } catch {
        // The page reports a failed move itself.
    }
}

function onDragEnd() {
    clearDrag();
}

function dropIndex(column, clientY, cardId) {
    const cards = [...column.querySelectorAll("[data-card-id]")]
        .filter(card => card.dataset.cardId !== cardId);

    for (let index = 0; index < cards.length; index++) {
        const rect = cards[index].getBoundingClientRect();
        if (clientY < rect.top + rect.height / 2) {
            return index;
        }
    }

    return cards.length;
}

function columnFrom(target) {
    return elementFrom(target)?.closest("[data-column-id]") ?? null;
}

function elementFrom(target) {
    if (target instanceof Element) {
        return target;
    }

    return target?.parentElement ?? null;
}

function placeSlot(column, clientY, cardId) {
    const list = column.querySelector("ol");
    if (!list) {
        return;
    }

    let slot = document.querySelector(".drop-slot");
    if (!slot) {
        slot = document.createElement("li");
        slot.className = "drop-slot";
        slot.setAttribute("aria-hidden", "true");
    }

    const cards = [...list.querySelectorAll("[data-card-id]")].filter(card => card.dataset.cardId !== cardId);
    let before = null;
    for (const card of cards) {
        const rect = card.getBoundingClientRect();
        if (clientY < rect.top + rect.height / 2) {
            before = card;
            break;
        }
    }

    if (before) {
        list.insertBefore(slot, before);
    } else {
        list.appendChild(slot);
    }
}

function clearDrag() {
    draggedCard?.classList.remove("dragging");
    document.querySelector(".drop-slot")?.remove();
    dragImage?.remove();
    draggedId = null;
    draggedCard = null;
    dragImage = null;
}
})();
