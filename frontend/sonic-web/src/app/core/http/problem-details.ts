export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;

    // stable custom extension key
    code?: string;
}
