export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;

    // backend sometimes uses this custom field
    code?: string;

    // Validation payloads (common shape)
    errors?: Record<string, string[]>;
}
